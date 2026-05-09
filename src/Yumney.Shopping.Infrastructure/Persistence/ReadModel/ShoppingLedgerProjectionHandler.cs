using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Persistence;
using SmartSolutionsLab.Yumney.Shared.Quantities;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore.Events;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;

/// <summary>
/// Async projection handler for the <c>ShoppingLedger</c> aggregate. Subscribes
/// to ledger integration events (<c>ShoppingItemAdded</c>, <c>Bought</c>,
/// <c>Consumed</c>, <c>Removed</c>, <c>QuantityAdjusted</c>) and maintains the
/// per-item rolled-up read row exposed via <see cref="MergedShoppingListDto"/>.
/// Distinct from <see cref="ShoppingListProjection"/>, which projects the
/// ShoppingList aggregate's own event stream.
/// See PROFILING.md alongside this file for the expected query budget per event.
/// All five handlers go through INSERT … ON CONFLICT keyed on
/// (OwnerId, lower(ItemName), COALESCE(Unit, '')) — the unique index added by
/// migration 20260509125057. That makes them order-independent (Bought arriving
/// before Added drops a stub instead of silently dropping the bought signal)
/// and concurrency-safe (two parallel ShoppingItemAdded for the same item
/// can't fan out to duplicate rows because the second INSERT collides on the
/// natural key and falls through to the merge UPDATE).
/// </summary>
public sealed class ShoppingLedgerProjectionHandler(ShoppingDbContext context)
	: IModuleEventHandler<ShoppingItemAddedModuleEvent>,
	  IModuleEventHandler<ShoppingItemBoughtModuleEvent>,
	  IModuleEventHandler<ShoppingItemConsumedModuleEvent>,
	  IModuleEventHandler<ShoppingItemRemovedModuleEvent>,
	  IModuleEventHandler<ShoppingItemQuantityAdjustedModuleEvent>
{
	public async Task HandleAsync(ShoppingItemAddedModuleEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		var quantity = inner.Quantity.Amount.Value;
		var unit = inner.Quantity.Unit?.Value;
		var category = (IngredientCategoryResolver.Resolve(inner.ItemName) ?? IngredientCategory.Other).Value;
		var sourceEntry = SerializeSource(quantity, inner.Source);
		var now = DateTime.UtcNow;

		// New row gets the full set; on conflict we merge into the existing row —
		// summing TotalQuantity and concatenating SourcesJson via jsonb || .
		// IsBought / BoughtAt are LEFT ALONE so an out-of-order Bought event
		// that already created a stub doesn't have its bought-state stomped.
		const string sql = """
			INSERT INTO "ShoppingListReadItems"
				("Id", "OwnerId", "ItemName", "TotalQuantity", "Unit", "Category", "IsBought", "BoughtAt", "SourcesJson", "LastUpdated")
			VALUES (@id, @ownerId, @itemName, @quantity, @unit, @category, FALSE, NULL, @sourceEntry, @now)
			ON CONFLICT ("OwnerId", lower("ItemName"), COALESCE("Unit", ''))
			DO UPDATE SET
				"TotalQuantity" = "ShoppingListReadItems"."TotalQuantity" + EXCLUDED."TotalQuantity",
				"SourcesJson" = "ShoppingListReadItems"."SourcesJson" || EXCLUDED."SourcesJson",
				"LastUpdated" = EXCLUDED."LastUpdated"
			""";

		await context.Database.ExecuteSqlRawAsync(
			sql,
			[
				new NpgsqlParameter("id", NpgsqlDbType.Uuid) { Value = Guid.CreateVersion7() },
				new NpgsqlParameter("ownerId", NpgsqlDbType.Text) { Value = @event.OwnerId },
				new NpgsqlParameter("itemName", NpgsqlDbType.Text) { Value = inner.ItemName.Value },
				new NpgsqlParameter("quantity", NpgsqlDbType.Numeric) { Value = quantity },
				new NpgsqlParameter("unit", NpgsqlDbType.Text) { Value = (object?)unit ?? DBNull.Value },
				new NpgsqlParameter("category", NpgsqlDbType.Text) { Value = category },
				new NpgsqlParameter("sourceEntry", NpgsqlDbType.Jsonb) { Value = sourceEntry },
				new NpgsqlParameter("now", NpgsqlDbType.TimestampTz) { Value = now },
			],
			cancellationToken);
	}

	public async Task HandleAsync(ShoppingItemBoughtModuleEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		var unit = inner.Quantity.Unit?.Value;
		var category = (IngredientCategoryResolver.Resolve(inner.ItemName) ?? IngredientCategory.Other).Value;
		var now = DateTime.UtcNow;

		// Bought-on-stub-or-real: insert a stub row with TotalQuantity=0 if
		// nothing exists yet (so the bought-state survives Added arriving later)
		// or flip IsBought / BoughtAt on the existing row. Quantity / sources
		// are deliberately untouched on conflict — they belong to Added /
		// Removed / QuantityAdjusted events.
		const string sql = """
			INSERT INTO "ShoppingListReadItems"
				("Id", "OwnerId", "ItemName", "TotalQuantity", "Unit", "Category", "IsBought", "BoughtAt", "SourcesJson", "LastUpdated")
			VALUES (@id, @ownerId, @itemName, 0, @unit, @category, TRUE, @now, '[]'::jsonb, @now)
			ON CONFLICT ("OwnerId", lower("ItemName"), COALESCE("Unit", ''))
			DO UPDATE SET
				"IsBought" = TRUE,
				"BoughtAt" = EXCLUDED."BoughtAt",
				"LastUpdated" = EXCLUDED."LastUpdated"
			""";

		await context.Database.ExecuteSqlRawAsync(
			sql,
			[
				new NpgsqlParameter("id", NpgsqlDbType.Uuid) { Value = Guid.CreateVersion7() },
				new NpgsqlParameter("ownerId", NpgsqlDbType.Text) { Value = @event.OwnerId },
				new NpgsqlParameter("itemName", NpgsqlDbType.Text) { Value = inner.ItemName.Value },
				new NpgsqlParameter("unit", NpgsqlDbType.Text) { Value = (object?)unit ?? DBNull.Value },
				new NpgsqlParameter("category", NpgsqlDbType.Text) { Value = category },
				new NpgsqlParameter("now", NpgsqlDbType.TimestampTz) { Value = now },
			],
			cancellationToken);
	}

	public async Task HandleAsync(ShoppingItemConsumedModuleEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		var unit = inner.Quantity.Unit?.Value;
		var category = (IngredientCategoryResolver.Resolve(inner.ItemName) ?? IngredientCategory.Other).Value;
		var now = DateTime.UtcNow;

		// Consume is a touch-only signal in the ledger projection (the legacy
		// Update-with-empty-mutate path). If the item isn't projected yet we
		// still drop a stub so a later Added doesn't surprise a downstream
		// reader that already saw the consumed event flow through; on conflict
		// we just bump LastUpdated.
		const string sql = """
			INSERT INTO "ShoppingListReadItems"
				("Id", "OwnerId", "ItemName", "TotalQuantity", "Unit", "Category", "IsBought", "BoughtAt", "SourcesJson", "LastUpdated")
			VALUES (@id, @ownerId, @itemName, 0, @unit, @category, FALSE, NULL, '[]'::jsonb, @now)
			ON CONFLICT ("OwnerId", lower("ItemName"), COALESCE("Unit", ''))
			DO UPDATE SET "LastUpdated" = EXCLUDED."LastUpdated"
			""";

		await context.Database.ExecuteSqlRawAsync(
			sql,
			[
				new NpgsqlParameter("id", NpgsqlDbType.Uuid) { Value = Guid.CreateVersion7() },
				new NpgsqlParameter("ownerId", NpgsqlDbType.Text) { Value = @event.OwnerId },
				new NpgsqlParameter("itemName", NpgsqlDbType.Text) { Value = inner.ItemName.Value },
				new NpgsqlParameter("unit", NpgsqlDbType.Text) { Value = (object?)unit ?? DBNull.Value },
				new NpgsqlParameter("category", NpgsqlDbType.Text) { Value = category },
				new NpgsqlParameter("now", NpgsqlDbType.TimestampTz) { Value = now },
			],
			cancellationToken);
	}

	public async Task HandleAsync(ShoppingItemRemovedModuleEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		var unit = inner.Quantity.Unit?.Value;
		var quantity = inner.Quantity.Amount.Value;
		var now = DateTime.UtcNow;

		// Decrement first via UPDATE (no UPSERT — removing nothing is a no-op,
		// not a reason to invent a stub row). Then DELETE the row if the total
		// dropped to <= 0; this matches the previous EF tracker semantics where
		// Remove was called when TotalQuantity went non-positive.
		await context.Database.ExecuteSqlRawAsync(
			"""
			UPDATE "ShoppingListReadItems"
			SET "TotalQuantity" = GREATEST(0, "TotalQuantity" - @quantity),
				"LastUpdated" = @now
			WHERE "OwnerId" = @ownerId
			  AND lower("ItemName") = lower(@itemName)
			  AND COALESCE("Unit", '') = COALESCE(@unit, '');
			""",
			[
				new NpgsqlParameter("ownerId", NpgsqlDbType.Text) { Value = @event.OwnerId },
				new NpgsqlParameter("itemName", NpgsqlDbType.Text) { Value = inner.ItemName.Value },
				new NpgsqlParameter("unit", NpgsqlDbType.Text) { Value = (object?)unit ?? DBNull.Value },
				new NpgsqlParameter("quantity", NpgsqlDbType.Numeric) { Value = quantity },
				new NpgsqlParameter("now", NpgsqlDbType.TimestampTz) { Value = now },
			],
			cancellationToken);

		await context.Database.ExecuteSqlRawAsync(
			"""
			DELETE FROM "ShoppingListReadItems"
			WHERE "OwnerId" = @ownerId
			  AND lower("ItemName") = lower(@itemName)
			  AND COALESCE("Unit", '') = COALESCE(@unit, '')
			  AND "TotalQuantity" <= 0;
			""",
			[
				new NpgsqlParameter("ownerId", NpgsqlDbType.Text) { Value = @event.OwnerId },
				new NpgsqlParameter("itemName", NpgsqlDbType.Text) { Value = inner.ItemName.Value },
				new NpgsqlParameter("unit", NpgsqlDbType.Text) { Value = (object?)unit ?? DBNull.Value },
			],
			cancellationToken);
	}

	public async Task HandleAsync(ShoppingItemQuantityAdjustedModuleEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		var unit = inner.NewQuantity.Unit?.Value;
		var category = (IngredientCategoryResolver.Resolve(inner.ItemName) ?? IngredientCategory.Other).Value;
		var quantity = inner.NewQuantity.Amount.Value;
		var now = DateTime.UtcNow;

		// QuantityAdjusted overwrites TotalQuantity rather than incrementing —
		// it carries the new absolute value, not a delta. On conflict we set
		// the absolute value too. If the row didn't exist yet we still drop a
		// stub at the new value so a later Added merges correctly.
		const string sql = """
			INSERT INTO "ShoppingListReadItems"
				("Id", "OwnerId", "ItemName", "TotalQuantity", "Unit", "Category", "IsBought", "BoughtAt", "SourcesJson", "LastUpdated")
			VALUES (@id, @ownerId, @itemName, @quantity, @unit, @category, FALSE, NULL, '[]'::jsonb, @now)
			ON CONFLICT ("OwnerId", lower("ItemName"), COALESCE("Unit", ''))
			DO UPDATE SET
				"TotalQuantity" = EXCLUDED."TotalQuantity",
				"LastUpdated" = EXCLUDED."LastUpdated"
			""";

		await context.Database.ExecuteSqlRawAsync(
			sql,
			[
				new NpgsqlParameter("id", NpgsqlDbType.Uuid) { Value = Guid.CreateVersion7() },
				new NpgsqlParameter("ownerId", NpgsqlDbType.Text) { Value = @event.OwnerId },
				new NpgsqlParameter("itemName", NpgsqlDbType.Text) { Value = inner.ItemName.Value },
				new NpgsqlParameter("quantity", NpgsqlDbType.Numeric) { Value = quantity },
				new NpgsqlParameter("unit", NpgsqlDbType.Text) { Value = (object?)unit ?? DBNull.Value },
				new NpgsqlParameter("category", NpgsqlDbType.Text) { Value = category },
				new NpgsqlParameter("now", NpgsqlDbType.TimestampTz) { Value = now },
			],
			cancellationToken);
	}

#pragma warning disable SA1204
	private static string SerializeSource(decimal quantity, string source)
	{
		var entry = new SourceEntry(quantity, source, DateTime.UtcNow);
		return JsonSerializer.Serialize<List<SourceEntry>>([entry]);
	}
#pragma warning restore SA1204

	private sealed record SourceEntry(decimal Quantity, string Source, DateTime OccurredAt);
}
