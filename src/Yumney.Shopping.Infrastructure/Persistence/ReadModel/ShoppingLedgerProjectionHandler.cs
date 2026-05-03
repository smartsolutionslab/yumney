using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;

/// <summary>
/// Async projection handler for the <c>ShoppingLedger</c> aggregate. Subscribes
/// to ledger integration events (<c>ShoppingItemAdded</c>, <c>Bought</c>,
/// <c>Consumed</c>, <c>Removed</c>, <c>QuantityAdjusted</c>) and maintains the
/// per-item rolled-up read row exposed via <see cref="MergedShoppingListDto"/>.
/// Distinct from <see cref="ShoppingListProjection"/>, which projects the
/// ShoppingList aggregate's own event stream.
/// See PROFILING.md alongside this file for the expected query budget per event.
/// </summary>
public sealed class ShoppingLedgerProjectionHandler(ShoppingDbContext context)
	: IModuleEventHandler<ShoppingItemAddedModuleEvent>,
	  IModuleEventHandler<ShoppingItemBoughtModuleEvent>,
	  IModuleEventHandler<ShoppingItemConsumedModuleEvent>,
	  IModuleEventHandler<ShoppingItemRemovedModuleEvent>,
	  IModuleEventHandler<ShoppingItemQuantityAdjustedModuleEvent>
{
	/// <inheritdoc />
	public Task HandleAsync(ShoppingItemAddedModuleEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;

		Action<ShoppingLedgerReadItem> mutate = readItem =>
		{
			readItem.TotalQuantity += inner.Quantity.Amount;
			AppendSource(readItem, inner.Quantity.Amount, inner.Source);
		};

		return UpsertAsync(@event.OwnerId, inner.ItemName, inner.Quantity.Unit?.Value, mutate, cancellationToken);
	}

	/// <inheritdoc />
	public Task HandleAsync(ShoppingItemBoughtModuleEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;

		Action<ShoppingLedgerReadItem> mutate = readItem =>
		{
			readItem.IsBought = true;
			readItem.BoughtAt = DateTime.UtcNow;
		};

		return UpdateAsync(@event.OwnerId, inner.ItemName, inner.Quantity.Unit?.Value, mutate, cancellationToken);
	}

	/// <inheritdoc />
	public Task HandleAsync(ShoppingItemConsumedModuleEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		return UpdateAsync(@event.OwnerId, inner.ItemName, inner.Quantity.Unit?.Value, _ => { }, cancellationToken);
	}

	/// <inheritdoc />
	public Task HandleAsync(ShoppingItemRemovedModuleEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;

		Action<ShoppingLedgerReadItem> mutate = readItem =>
		{
			readItem.TotalQuantity = Math.Max(0, readItem.TotalQuantity - inner.Quantity.Amount);
			if (readItem.TotalQuantity <= 0)
			{
				context.Set<ShoppingLedgerReadItem>().Remove(readItem);
			}
		};

		return UpdateAsync(@event.OwnerId, inner.ItemName, inner.Quantity.Unit?.Value, mutate, cancellationToken);
	}

	/// <inheritdoc />
	public Task HandleAsync(ShoppingItemQuantityAdjustedModuleEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;

		Action<ShoppingLedgerReadItem> mutate = readItem =>
		{
			readItem.TotalQuantity = inner.NewQuantity.Amount;
		};

		return UpdateAsync(@event.OwnerId, inner.ItemName, inner.NewQuantity.Unit?.Value, mutate, cancellationToken);
	}

	private async Task UpdateAsync(
		string ownerId,
		string itemName,
		string? unit,
		Action<ShoppingLedgerReadItem> mutate,
		CancellationToken cancellationToken)
	{
		var readItem = await FindAsync(ownerId, itemName, unit, cancellationToken);
		if (readItem is null) return;

		mutate(readItem);
		readItem.LastUpdated = DateTime.UtcNow;
		await context.SaveChangesAsync(cancellationToken);
	}

	private async Task UpsertAsync(
		string ownerId,
		string itemName,
		string? unit,
		Action<ShoppingLedgerReadItem> mutate,
		CancellationToken cancellationToken)
	{
		var readItem = await FindAsync(ownerId, itemName, unit, cancellationToken)
			?? Create(ownerId, itemName, unit);
		mutate(readItem);
		readItem.LastUpdated = DateTime.UtcNow;
		await context.SaveChangesAsync(cancellationToken);
	}

	private ShoppingLedgerReadItem Create(string ownerId, string itemName, string? unit)
	{
		var category = IngredientCategoryResolver.Resolve(itemName) ?? IngredientCategory.Other;
		var readItem = new ShoppingLedgerReadItem
		{
			Id = Guid.CreateVersion7(),
			OwnerId = ownerId,
			ItemName = itemName,
			Unit = unit,
			Category = category.Value,
			LastUpdated = DateTime.UtcNow,
		};
		context.Set<ShoppingLedgerReadItem>().Add(readItem);
		return readItem;
	}

	private async Task<ShoppingLedgerReadItem?> FindAsync(string ownerId, string itemName, string? unit, CancellationToken cancellationToken)
	{
		return await context.Set<ShoppingLedgerReadItem>()
			.FirstOrDefaultAsync(
				r => r.OwnerId == ownerId
					&& EF.Functions.ILike(r.ItemName, itemName)
					&& r.Unit == unit,
				cancellationToken);
	}

#pragma warning disable SA1204
	private static void AppendSource(ShoppingLedgerReadItem readItem, decimal quantity, string source)
	{
		var sources = JsonSerializer.Deserialize<List<SourceEntry>>(readItem.SourcesJson) ?? [];
		sources.Add(new SourceEntry(quantity, source, DateTime.UtcNow));
		readItem.SourcesJson = JsonSerializer.Serialize(sources);
	}

	private sealed record SourceEntry(decimal Quantity, string Source, DateTime OccurredAt);
}
