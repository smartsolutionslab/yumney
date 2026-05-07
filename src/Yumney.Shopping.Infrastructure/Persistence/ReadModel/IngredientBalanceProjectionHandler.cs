using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Persistence;
using SmartSolutionsLab.Yumney.Shared.Quantities;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;

/// <summary>
/// Async projection handler that maintains per-ingredient at-home balance rows
/// in <see cref="IngredientBalanceReadItem"/>. Subscribes to ledger integration
/// events that affect the Bought / Consumed / Removed totals.
///
/// Powers the <see cref="GetIngredientBalanceQuery"/> ("what's at home right now")
/// and is the data source for the "What Can I Cook?" feature (US-342).
/// </summary>
public sealed class IngredientBalanceProjectionHandler(ShoppingDbContext context, TimeProvider timeProvider)
	: IModuleEventHandler<ShoppingItemBoughtModuleEvent>,
	  IModuleEventHandler<ShoppingItemConsumedModuleEvent>,
	  IModuleEventHandler<ShoppingItemRemovedModuleEvent>,
	  IModuleEventHandler<ShoppingItemUndoBoughtModuleEvent>,
	  IModuleEventHandler<ShoppingItemAddedAsAtHomeModuleEvent>,
	  IModuleEventHandler<ShoppingItemMarkedAsFrozenModuleEvent>
{
	public Task HandleAsync(ShoppingItemBoughtModuleEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		var now = timeProvider.GetUtcNow().UtcDateTime;
		return UpsertAsync(
			@event.OwnerId,
			inner.ItemName,
			inner.Quantity.Unit?.Value,
			row =>
			{
				row.BoughtTotal += inner.Quantity.Amount;
				row.LastBoughtAt = now;
			},
			cancellationToken);
	}

	public Task HandleAsync(ShoppingItemConsumedModuleEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		return UpsertAsync(
			@event.OwnerId,
			inner.ItemName,
			inner.Quantity.Unit?.Value,
			row => row.ConsumedTotal += inner.Quantity.Amount,
			cancellationToken);
	}

	public Task HandleAsync(ShoppingItemRemovedModuleEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		return UpsertAsync(
			@event.OwnerId,
			inner.ItemName,
			inner.Quantity.Unit?.Value,
			row => row.RemovedTotal += inner.Quantity.Amount,
			cancellationToken);
	}

	public Task HandleAsync(ShoppingItemUndoBoughtModuleEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		return UpsertAsync(
			@event.OwnerId,
			inner.ItemName,
			inner.Quantity.Unit?.Value,
			row => row.BoughtTotal = Math.Max(0, row.BoughtTotal - inner.Quantity.Amount),
			cancellationToken);
	}

	public Task HandleAsync(ShoppingItemAddedAsAtHomeModuleEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		var now = timeProvider.GetUtcNow().UtcDateTime;
		return UpsertAsync(
			@event.OwnerId,
			inner.ItemName,
			inner.Quantity.Unit?.Value,
			row =>
			{
				row.BoughtTotal += inner.Quantity.Amount;
				row.LastBoughtAt = now;
			},
			cancellationToken);
	}

	public Task HandleAsync(ShoppingItemMarkedAsFrozenModuleEvent @event, CancellationToken cancellationToken = default)
	{
		// Pure update: don't create a row if the item isn't on the ledger —
		// freezing a non-existent item is a no-op rather than a phantom entry.
		var inner = @event.Inner;
		var nameKey = inner.ItemName.Value.ToLowerInvariant();
		var unit = inner.Unit?.Value;
		var now = timeProvider.GetUtcNow().UtcDateTime;
		return context.UpdateAsync<IngredientBalanceReadItem>(
			row => row.OwnerId == @event.OwnerId && row.NameKey == nameKey && row.Unit == unit,
			row =>
			{
				row.Category = IngredientCategory.Frozen.Value;
				row.LastBoughtAt = now;
				row.LastUpdated = now;
			},
			cancellationToken);
	}

	private Task UpsertAsync(
		string ownerId,
		string itemName,
		string? unit,
		Action<IngredientBalanceReadItem> mutate,
		CancellationToken cancellationToken)
	{
		var nameKey = itemName.ToLowerInvariant();
		return context.UpsertAsync<IngredientBalanceReadItem>(
			row => row.OwnerId == ownerId && row.NameKey == nameKey && row.Unit == unit,
			() => new IngredientBalanceReadItem
			{
				Id = Guid.CreateVersion7(),
				OwnerId = ownerId,
				ItemName = itemName,
				NameKey = nameKey,
				Unit = unit,
				Category = (IngredientCategoryResolver.Resolve(itemName) ?? IngredientCategory.Other).Value,
			},
			row =>
			{
				mutate(row);
				row.LastUpdated = timeProvider.GetUtcNow().UtcDateTime;
			},
			cancellationToken);
	}
}
