using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore.Events;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;

/// <summary>
/// Async projection handler — rebuilds the ShoppingList read model from
/// integration events published by the event store. Idempotent upserts so
/// replay produces the same final state.
/// </summary>
public sealed class ShoppingListProjection(ShoppingDbContext context)
	: IModuleEventHandler<ShoppingListCreatedModuleEvent>,
	  IModuleEventHandler<ListItemAddedModuleEvent>,
	  IModuleEventHandler<ListItemCheckedModuleEvent>,
	  IModuleEventHandler<ListItemUncheckedModuleEvent>,
	  IModuleEventHandler<ListItemCategoryChangedModuleEvent>,
	  IModuleEventHandler<AllItemsCheckedModuleEvent>,
	  IModuleEventHandler<AllItemsUncheckedModuleEvent>,
	  IModuleEventHandler<RecipeReferenceClearedModuleEvent>
{
	public async Task HandleAsync(ShoppingListCreatedModuleEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;

		// UPSERT-by-metadata: an early-arriving ListItemAdded may already have inserted
		// the row with empty Title and ItemCount > 0 (see ListItemAdded handler). We
		// fill in OwnerId/Title/RecipeIdentifier/CreatedAt without ever touching
		// ItemCount, so the count populated by ListItemAdded handlers is preserved.
		// EXCLUDED here is the row we tried to INSERT; ON CONFLICT (Id) means an
		// existing row keeps its primary key and we only overwrite the metadata
		// columns explicitly listed in the SET clause.
		// Explicit Npgsql parameters because EF Core's FormattableString path can't
		// infer a column type from a `Guid?` whose runtime value is null.
		const string sql = """
			INSERT INTO "ShoppingListSummaryReadItems"
				("Id", "OwnerId", "Title", "RecipeIdentifier", "ItemCount", "CreatedAt", "LastUpdated")
			VALUES (@id, @ownerId, @title, @recipeId, 0, @createdAt, @createdAt)
			ON CONFLICT ("Id") DO UPDATE
			SET "OwnerId" = EXCLUDED."OwnerId",
				"Title" = EXCLUDED."Title",
				"RecipeIdentifier" = EXCLUDED."RecipeIdentifier",
				"CreatedAt" = EXCLUDED."CreatedAt",
				"LastUpdated" = EXCLUDED."LastUpdated"
			""";

		await context.Database.ExecuteSqlRawAsync(
			sql,
			[
				new NpgsqlParameter("id", NpgsqlDbType.Uuid) { Value = @event.AggregateId },
				new NpgsqlParameter("ownerId", NpgsqlDbType.Text) { Value = @event.OwnerId },
				new NpgsqlParameter("title", NpgsqlDbType.Text) { Value = inner.Title.Value },
				new NpgsqlParameter("recipeId", NpgsqlDbType.Uuid)
				{
					Value = (object?)inner.RecipeReference?.Value ?? DBNull.Value,
				},
				new NpgsqlParameter("createdAt", NpgsqlDbType.TimestampTz) { Value = inner.CreatedAt },
			],
			cancellationToken);
	}

	public async Task HandleAsync(ListItemAddedModuleEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;

		// UPSERT-with-stub-recovery: ListItemChecked / ListItemCategoryChanged
		// fan out on different RabbitMQ queues and can arrive ahead of
		// ListItemAdded — they create a stub row keyed by ItemId so their state
		// isn't dropped (see the SetCheckedAsync / ChangeCategoryAsync helpers
		// below). The WHERE clause on the conflict path filters: only stubs
		// (Name = '') get filled in; a redelivery against a fully-projected row
		// becomes a no-op. RETURNING then emits TRUE iff this invocation
		// produced the first full projection (insert OR stub-fill) — that's
		// what gates the summary's ItemCount bump. No RETURNING row → no count
		// drift on redelivery.
		var now = DateTime.UtcNow;
		const string upsertSql = """
			INSERT INTO "ShoppingListItemReadItems"
				("Id", "ListId", "OwnerId", "Name", "QuantityAmount", "QuantityUnit", "Category", "IsChecked", "CreatedAt", "LastUpdated")
			VALUES (@id, @listId, @ownerId, @name, @amount, @unit, @category, FALSE, @now, @now)
			ON CONFLICT ("Id") DO UPDATE
			SET "ListId" = EXCLUDED."ListId",
				"OwnerId" = EXCLUDED."OwnerId",
				"Name" = EXCLUDED."Name",
				"QuantityAmount" = EXCLUDED."QuantityAmount",
				"QuantityUnit" = EXCLUDED."QuantityUnit",
				"Category" = EXCLUDED."Category",
				"LastUpdated" = EXCLUDED."LastUpdated"
			WHERE "ShoppingListItemReadItems"."Name" = ''
			RETURNING TRUE AS "Value"
			""";

		var firstFullProjection = await context.Database
			.SqlQueryRaw<bool>(
				upsertSql,
				new NpgsqlParameter("id", NpgsqlDbType.Uuid) { Value = inner.ItemId.Value },
				new NpgsqlParameter("listId", NpgsqlDbType.Uuid) { Value = @event.AggregateId },
				new NpgsqlParameter("ownerId", NpgsqlDbType.Text) { Value = @event.OwnerId },
				new NpgsqlParameter("name", NpgsqlDbType.Text) { Value = inner.Name.Value },
				new NpgsqlParameter("amount", NpgsqlDbType.Numeric)
				{
					Value = (object?)inner.Quantity?.Amount.Value ?? DBNull.Value,
				},
				new NpgsqlParameter("unit", NpgsqlDbType.Text)
				{
					Value = (object?)inner.Quantity?.Unit?.Value ?? DBNull.Value,
				},
				new NpgsqlParameter("category", NpgsqlDbType.Text)
				{
					Value = (inner.Category ?? Shared.Quantities.IngredientCategory.Other).Value,
				},
				new NpgsqlParameter("now", NpgsqlDbType.TimestampTz) { Value = now })
			.AsAsyncEnumerable()
			.FirstOrDefaultAsync(cancellationToken);

		// RETURNING was empty: either redelivery against a fully-projected row, or
		// the WHERE clause filtered out the no-op update. Either way nothing new
		// to count; skip the summary bump.
		if (!firstFullProjection) return;

		// UPSERT-by-count: ListItemAdded and ShoppingListCreated module events fan
		// out to separate RabbitMQ queues with no cross-queue ordering, so an item
		// may arrive before the summary row exists. INSERT … ON CONFLICT keeps the
		// increment idempotent across both orderings: a missing row gets created
		// with ItemCount=1; an existing one is incremented atomically under
		// PostgreSQL's row-level UPDATE lock so concurrent adds for the same list
		// serialize cleanly. The placeholder Title='' on insert is overwritten by
		// the ShoppingListCreated handler's UPSERT when it eventually runs.
		const string summarySql = """
			INSERT INTO "ShoppingListSummaryReadItems"
				("Id", "OwnerId", "Title", "RecipeIdentifier", "ItemCount", "CreatedAt", "LastUpdated")
			VALUES (@id, @ownerId, '', NULL, 1, @now, @now)
			ON CONFLICT ("Id") DO UPDATE
			SET "ItemCount" = "ShoppingListSummaryReadItems"."ItemCount" + 1,
				"LastUpdated" = EXCLUDED."LastUpdated"
			""";

		await context.Database.ExecuteSqlRawAsync(
			summarySql,
			[
				new NpgsqlParameter("id", NpgsqlDbType.Uuid) { Value = @event.AggregateId },
				new NpgsqlParameter("ownerId", NpgsqlDbType.Text) { Value = @event.OwnerId },
				new NpgsqlParameter("now", NpgsqlDbType.TimestampTz) { Value = now },
			],
			cancellationToken);
	}

	public Task HandleAsync(ListItemCheckedModuleEvent @event, CancellationToken cancellationToken = default) =>
		UpsertItemCheckedStateAsync(@event.AggregateId, @event.OwnerId, @event.Inner.ItemId.Value, true, cancellationToken);

	public Task HandleAsync(ListItemUncheckedModuleEvent @event, CancellationToken cancellationToken = default) =>
		UpsertItemCheckedStateAsync(@event.AggregateId, @event.OwnerId, @event.Inner.ItemId.Value, false, cancellationToken);

	public async Task HandleAsync(AllItemsCheckedModuleEvent @event, CancellationToken cancellationToken = default)
	{
		await SetAllCheckedAsync(@event.AggregateId, true, cancellationToken);
		await TouchSummaryAsync(@event.AggregateId, cancellationToken);
		await context.SaveChangesAsync(cancellationToken);
	}

	public async Task HandleAsync(AllItemsUncheckedModuleEvent @event, CancellationToken cancellationToken = default)
	{
		await SetAllCheckedAsync(@event.AggregateId, false, cancellationToken);
		await TouchSummaryAsync(@event.AggregateId, cancellationToken);
		await context.SaveChangesAsync(cancellationToken);
	}

	public async Task HandleAsync(ListItemCategoryChangedModuleEvent @event, CancellationToken cancellationToken = default)
	{
		// UPSERT pattern matches ListItemAdded — a category change can race
		// ListItemAdded on the wire; if it lands first, drop a stub so the
		// category isn't lost when ListItemAdded later fills the rest in.
		var now = DateTime.UtcNow;
		const string sql = """
			INSERT INTO "ShoppingListItemReadItems"
				("Id", "ListId", "OwnerId", "Name", "QuantityAmount", "QuantityUnit", "Category", "IsChecked", "CreatedAt", "LastUpdated")
			VALUES (@id, @listId, @ownerId, '', NULL, NULL, @category, FALSE, @now, @now)
			ON CONFLICT ("Id") DO UPDATE
			SET "Category" = EXCLUDED."Category",
				"LastUpdated" = EXCLUDED."LastUpdated"
			""";

		await context.Database.ExecuteSqlRawAsync(
			sql,
			[
				new NpgsqlParameter("id", NpgsqlDbType.Uuid) { Value = @event.Inner.ItemId.Value },
				new NpgsqlParameter("listId", NpgsqlDbType.Uuid) { Value = @event.AggregateId },
				new NpgsqlParameter("ownerId", NpgsqlDbType.Text) { Value = @event.OwnerId },
				new NpgsqlParameter("category", NpgsqlDbType.Text) { Value = @event.Inner.Category.Value },
				new NpgsqlParameter("now", NpgsqlDbType.TimestampTz) { Value = now },
			],
			cancellationToken);
	}

	public async Task HandleAsync(RecipeReferenceClearedModuleEvent @event, CancellationToken cancellationToken = default)
	{
		var summary = await context.Set<ShoppingListSummaryReadItem>()
			.FirstOrDefaultAsync(s => s.Id == @event.AggregateId, cancellationToken);
		if (summary is null) return;

		summary.RecipeIdentifier = null;
		summary.LastUpdated = DateTime.UtcNow;
		await context.SaveChangesAsync(cancellationToken);
	}

	private async Task UpsertItemCheckedStateAsync(
		Guid listId,
		string ownerId,
		Guid itemId,
		bool isChecked,
		CancellationToken cancellationToken)
	{
		// UPSERT pattern matches ListItemAdded — the check/uncheck state must
		// survive the case where this event arrives before ListItemAdded does.
		// When that happens, drop a stub (empty Name placeholder) so the
		// IsChecked signal isn't dropped; ListItemAdded later fills the rest in
		// while preserving IsChecked (its UPSERT touches Name/Quantity/Category
		// only).
		var now = DateTime.UtcNow;
		const string sql = """
			INSERT INTO "ShoppingListItemReadItems"
				("Id", "ListId", "OwnerId", "Name", "QuantityAmount", "QuantityUnit", "Category", "IsChecked", "CreatedAt", "LastUpdated")
			VALUES (@id, @listId, @ownerId, '', NULL, NULL, 'other', @isChecked, @now, @now)
			ON CONFLICT ("Id") DO UPDATE
			SET "IsChecked" = EXCLUDED."IsChecked",
				"LastUpdated" = EXCLUDED."LastUpdated"
			""";

		await context.Database.ExecuteSqlRawAsync(
			sql,
			[
				new NpgsqlParameter("id", NpgsqlDbType.Uuid) { Value = itemId },
				new NpgsqlParameter("listId", NpgsqlDbType.Uuid) { Value = listId },
				new NpgsqlParameter("ownerId", NpgsqlDbType.Text) { Value = ownerId },
				new NpgsqlParameter("isChecked", NpgsqlDbType.Boolean) { Value = isChecked },
				new NpgsqlParameter("now", NpgsqlDbType.TimestampTz) { Value = now },
			],
			cancellationToken);
	}

	private async Task SetAllCheckedAsync(Guid listId, bool isChecked, CancellationToken cancellationToken)
	{
		// SQL-side bulk update — no per-item materialise. Runs in its own
		// transaction (ExecuteUpdateAsync doesn't enlist the change tracker)
		// which is fine here because the surrounding handler does its own
		// SaveChangesAsync only to commit the touched summary row.
		var now = DateTime.UtcNow;
		await context.Set<ShoppingListItemReadItem>()
			.Where(item => item.ListId == listId)
			.ExecuteUpdateAsync(
				setters => setters
					.SetProperty(item => item.IsChecked, isChecked)
					.SetProperty(item => item.LastUpdated, now),
				cancellationToken);
	}

	private async Task TouchSummaryAsync(Guid listId, CancellationToken cancellationToken)
	{
		var summary = await context.Set<ShoppingListSummaryReadItem>()
			.FirstOrDefaultAsync(s => s.Id == listId, cancellationToken);
		if (summary is null) return;
		summary.LastUpdated = DateTime.UtcNow;
	}
}
