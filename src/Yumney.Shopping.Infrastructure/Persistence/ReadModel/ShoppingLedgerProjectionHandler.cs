using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;
using SmartSolutionsLab.Yumney.Shared.Quantities;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore.Events;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;

/// <summary>
/// Async projection handler for the <c>ShoppingLedger</c> aggregate. Maintains
/// the per-item rolled-up read row exposed via <see cref="MergedShoppingListDto"/>
/// from ledger integration events (<c>ShoppingItemAdded</c>, <c>Bought</c>,
/// <c>Consumed</c>, <c>Removed</c>, <c>QuantityAdjusted</c>).
/// Distinct from <see cref="ShoppingListProjection"/>, which projects the
/// ShoppingList aggregate's own event stream. See PROFILING.md alongside this
/// file for the expected query budget per event.
///
/// All five handlers are race-safe under Wolverine's per-event-type queue
/// fan-out: the unique index from migration 20260509125057 means a second
/// concurrent insert for the same (OwnerId, lower(ItemName), Unit) collides
/// and the catch falls through to the merge UPDATE. They're also order-
/// independent — a Bought / Consumed / QuantityAdjusted that arrives before
/// the matching Added drops a stub row so the signal isn't dropped on the
/// floor.
///
/// EF Core does the heavy lifting via tracker INSERTs and ExecuteUpdateAsync /
/// ExecuteDeleteAsync. The single piece of raw SQL is the <c>jsonb ||</c>
/// concat in the Added merge path: there's no EF-native equivalent for
/// appending to a jsonb array column without a deserialise/append/serialise
/// round-trip that would race.
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
		var ownerId = @event.OwnerId;
		var itemName = inner.ItemName.Value;
		var category = (IngredientCategoryResolver.Resolve(itemName) ?? IngredientCategory.Other).Value;
		var sourcesJson = SerializeSources(quantity, inner.Source);
		var now = DateTime.UtcNow;

		// Optimistic insert via the EF tracker. The natural-key unique index
		// turns a duplicate into a DbUpdateException that the catch routes to
		// the merge path; on the happy fresh-row path no extra round-trip.
		context.Set<ShoppingLedgerReadItem>().Add(new ShoppingLedgerReadItem
		{
			Id = Guid.CreateVersion7(),
			OwnerId = ownerId,
			ItemName = itemName,
			TotalQuantity = quantity,
			Unit = unit,
			Category = category,
			IsBought = false,
			BoughtAt = null,
			SourcesJson = sourcesJson,
			LastUpdated = now,
		});

		try
		{
			await context.SaveChangesAsync(cancellationToken);
			return;
		}
		catch (DbUpdateException ex) when (ex.IsUniqueViolation())
		{
			// Existing row — drop the staged insert and fall through to merge.
			context.ChangeTracker.Clear();
		}

		// Sum the quantity in EF (atomic via ExecuteUpdate's row lock) and
		// concatenate the new source onto the jsonb array via raw SQL — EF
		// can't model jsonb || jsonb without a deserialise round-trip. The
		// two statements share the implicit Wolverine handler transaction, so
		// the merge is atomic from a downstream reader's perspective.
		await context.Set<ShoppingLedgerReadItem>()
			.Where(row => row.OwnerId == ownerId
				&& EF.Functions.ILike(row.ItemName, itemName)
				&& row.Unit == unit)
			.ExecuteUpdateAsync(
				setters =>
				{
					setters.SetProperty(row => row.TotalQuantity, row => row.TotalQuantity + quantity);
					setters.SetProperty(row => row.LastUpdated, _ => now);
				},
				cancellationToken);

		await context.Database.ExecuteSqlInterpolatedAsync(
			$"""
			UPDATE "ShoppingListReadItems"
			SET "SourcesJson" = "SourcesJson" || {sourcesJson}::jsonb
			WHERE "OwnerId" = {ownerId}
			  AND lower("ItemName") = lower({itemName})
			  AND COALESCE("Unit", '') = COALESCE({unit}, '')
			""",
			cancellationToken);
	}

	public Task HandleAsync(ShoppingItemBoughtModuleEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		var now = DateTime.UtcNow;
		return UpsertStateAsync(
			@event.OwnerId,
			inner.ItemName,
			inner.Quantity.Unit?.Value,
			updateExisting: setters =>
			{
				setters.SetProperty(row => row.IsBought, true);
				setters.SetProperty(row => row.BoughtAt, _ => now);
				setters.SetProperty(row => row.LastUpdated, _ => now);
			},
			stubFromExisting: stub =>
			{
				stub.IsBought = true;
				stub.BoughtAt = now;
			},
			cancellationToken);
	}

	public Task HandleAsync(ShoppingItemConsumedModuleEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		var now = DateTime.UtcNow;
		return UpsertStateAsync(
			@event.OwnerId,
			inner.ItemName,
			inner.Quantity.Unit?.Value,
			updateExisting: setters =>
			{
				setters.SetProperty(row => row.LastUpdated, _ => now);
			},
			stubFromExisting: _ => { },
			cancellationToken);
	}

	public async Task HandleAsync(ShoppingItemRemovedModuleEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		var ownerId = @event.OwnerId;
		var itemName = inner.ItemName.Value;
		var unit = inner.Quantity.Unit?.Value;
		var quantity = inner.Quantity.Amount.Value;
		var now = DateTime.UtcNow;

		// Decrement first; if the running total drops to <=0 the second pass
		// deletes the row. No UPSERT — removing nothing is a no-op, not a
		// reason to invent a stub.
		await context.Set<ShoppingLedgerReadItem>()
			.Where(row => row.OwnerId == ownerId
				&& EF.Functions.ILike(row.ItemName, itemName)
				&& row.Unit == unit)
			.ExecuteUpdateAsync(
				setters =>
				{
					setters.SetProperty(row => row.TotalQuantity, row => row.TotalQuantity > quantity ? row.TotalQuantity - quantity : 0);
					setters.SetProperty(row => row.LastUpdated, _ => now);
				},
				cancellationToken);

		await context.Set<ShoppingLedgerReadItem>()
			.Where(row => row.OwnerId == ownerId
				&& EF.Functions.ILike(row.ItemName, itemName)
				&& row.Unit == unit
				&& row.TotalQuantity <= 0)
			.ExecuteDeleteAsync(cancellationToken);
	}

	public Task HandleAsync(ShoppingItemQuantityAdjustedModuleEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		var newQuantity = inner.NewQuantity.Amount.Value;
		var now = DateTime.UtcNow;
		return UpsertStateAsync(
			@event.OwnerId,
			inner.ItemName,
			inner.NewQuantity.Unit?.Value,
			updateExisting: setters =>
			{
				setters.SetProperty(row => row.TotalQuantity, _ => newQuantity);
				setters.SetProperty(row => row.LastUpdated, _ => now);
			},
			stubFromExisting: stub =>
			{
				stub.TotalQuantity = newQuantity;
			},
			cancellationToken);
	}

	private async Task UpsertStateAsync(
		string ownerId,
		string itemName,
		string? unit,
		Action<UpdateSettersBuilder<ShoppingLedgerReadItem>> updateExisting,
		Action<ShoppingLedgerReadItem> stubFromExisting,
		CancellationToken cancellationToken)
	{
		// Try to update an existing row first. ExecuteUpdate is a single
		// SQL UPDATE: race-safe against concurrent updates of the same row
		// (Postgres row-level locks serialise them) and idempotent against
		// redelivery (running it twice produces the same end state).
		var rowsAffected = await context.Set<ShoppingLedgerReadItem>()
			.Where(row => row.OwnerId == ownerId
				&& EF.Functions.ILike(row.ItemName, itemName)
				&& row.Unit == unit)
			.ExecuteUpdateAsync(updateExisting, cancellationToken);

		if (rowsAffected > 0) return;

		// No matching row — drop a stub keyed by the natural key so the
		// state isn't dropped if Added arrives later. The unique constraint
		// catches the race against another concurrent stub-insert; on
		// collision we fall back to the UPDATE we just did, which now has
		// a row to land on.
		var stub = new ShoppingLedgerReadItem
		{
			Id = Guid.CreateVersion7(),
			OwnerId = ownerId,
			ItemName = itemName,
			TotalQuantity = 0,
			Unit = unit,
			Category = (IngredientCategoryResolver.Resolve(itemName) ?? IngredientCategory.Other).Value,
			IsBought = false,
			BoughtAt = null,
			SourcesJson = "[]",
			LastUpdated = DateTime.UtcNow,
		};
		stubFromExisting(stub);
		context.Set<ShoppingLedgerReadItem>().Add(stub);

		try
		{
			await context.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException ex) when (ex.IsUniqueViolation())
		{
			context.ChangeTracker.Clear();
			await context.Set<ShoppingLedgerReadItem>()
				.Where(row => row.OwnerId == ownerId
					&& EF.Functions.ILike(row.ItemName, itemName)
					&& row.Unit == unit)
				.ExecuteUpdateAsync(updateExisting, cancellationToken);
		}
	}

#pragma warning disable SA1204
	private static string SerializeSources(decimal quantity, string source)
	{
		var entry = new SourceEntry(quantity, source, DateTime.UtcNow);
		return JsonSerializer.Serialize<List<SourceEntry>>([entry]);
	}
#pragma warning restore SA1204

	private sealed record SourceEntry(decimal Quantity, string Source, DateTime OccurredAt);
}
