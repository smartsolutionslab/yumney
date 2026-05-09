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
		var existing = await context.Set<ShoppingListItemReadItem>()
			.FirstOrDefaultAsync(i => i.Id == inner.ItemId.Value, cancellationToken);

		// Idempotency guard — replay or RabbitMQ redelivery should not double-insert
		// the item row or double-bump the summary's ItemCount.
		if (existing is not null) return;

		context.Set<ShoppingListItemReadItem>().Add(new ShoppingListItemReadItem
		{
			Id = inner.ItemId.Value,
			ListId = @event.AggregateId,
			OwnerId = @event.OwnerId,
			Name = inner.Name.Value,
			QuantityAmount = inner.Quantity?.Amount.Value,
			QuantityUnit = inner.Quantity?.Unit?.Value,
			Category = (inner.Category ?? Shared.Quantities.IngredientCategory.Other).Value,
			IsChecked = false,
			CreatedAt = DateTime.UtcNow,
			LastUpdated = DateTime.UtcNow,
		});
		await context.SaveChangesAsync(cancellationToken);

		// UPSERT-by-count: ListItemAdded and ShoppingListCreated module events fan
		// out to separate RabbitMQ queues with no cross-queue ordering, so an item
		// may arrive before the summary row exists. INSERT … ON CONFLICT keeps the
		// increment idempotent across both orderings: a missing row gets created
		// with ItemCount=1; an existing one is incremented atomically under
		// PostgreSQL's row-level UPDATE lock so concurrent adds for the same list
		// serialize cleanly. The placeholder Title='' on insert is overwritten by
		// the ShoppingListCreated handler's UPSERT when it eventually runs.
		var now = DateTime.UtcNow;
		const string sql = """
			INSERT INTO "ShoppingListSummaryReadItems"
				("Id", "OwnerId", "Title", "RecipeIdentifier", "ItemCount", "CreatedAt", "LastUpdated")
			VALUES (@id, @ownerId, '', NULL, 1, @now, @now)
			ON CONFLICT ("Id") DO UPDATE
			SET "ItemCount" = "ShoppingListSummaryReadItems"."ItemCount" + 1,
				"LastUpdated" = EXCLUDED."LastUpdated"
			""";

		await context.Database.ExecuteSqlRawAsync(
			sql,
			[
				new NpgsqlParameter("id", NpgsqlDbType.Uuid) { Value = @event.AggregateId },
				new NpgsqlParameter("ownerId", NpgsqlDbType.Text) { Value = @event.OwnerId },
				new NpgsqlParameter("now", NpgsqlDbType.TimestampTz) { Value = now },
			],
			cancellationToken);
	}

	public async Task HandleAsync(ListItemCheckedModuleEvent @event, CancellationToken cancellationToken = default)
	{
		await SetCheckedAsync(@event.Inner.ItemId.Value, true, cancellationToken);
		await TouchSummaryAsync(@event.AggregateId, cancellationToken);
		await context.SaveChangesAsync(cancellationToken);
	}

	public async Task HandleAsync(ListItemUncheckedModuleEvent @event, CancellationToken cancellationToken = default)
	{
		await SetCheckedAsync(@event.Inner.ItemId.Value, false, cancellationToken);
		await TouchSummaryAsync(@event.AggregateId, cancellationToken);
		await context.SaveChangesAsync(cancellationToken);
	}

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
		var item = await context.Set<ShoppingListItemReadItem>()
			.FirstOrDefaultAsync(i => i.Id == @event.Inner.ItemId.Value, cancellationToken);
		if (item is null) return;

		item.Category = @event.Inner.Category.Value;
		item.LastUpdated = DateTime.UtcNow;
		await TouchSummaryAsync(@event.AggregateId, cancellationToken);
		await context.SaveChangesAsync(cancellationToken);
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

	private async Task SetCheckedAsync(Guid itemId, bool isChecked, CancellationToken cancellationToken)
	{
		var item = await context.Set<ShoppingListItemReadItem>()
			.FirstOrDefaultAsync(i => i.Id == itemId, cancellationToken);
		if (item is null) return;
		item.IsChecked = isChecked;
		item.LastUpdated = DateTime.UtcNow;
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
