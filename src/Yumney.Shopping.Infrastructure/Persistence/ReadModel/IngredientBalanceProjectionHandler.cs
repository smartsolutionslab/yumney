using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Events;
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
	: IIntegrationEventHandler<ShoppingItemBoughtIntegrationEvent>,
	  IIntegrationEventHandler<ShoppingItemConsumedIntegrationEvent>,
	  IIntegrationEventHandler<ShoppingItemRemovedIntegrationEvent>,
	  IIntegrationEventHandler<ShoppingItemUndoBoughtIntegrationEvent>,
	  IIntegrationEventHandler<ShoppingItemAddedAsAtHomeIntegrationEvent>
{
	public Task HandleAsync(ShoppingItemBoughtIntegrationEvent @event, CancellationToken cancellationToken = default)
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

	public Task HandleAsync(ShoppingItemConsumedIntegrationEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		return UpsertAsync(
			@event.OwnerId,
			inner.ItemName,
			inner.Quantity.Unit?.Value,
			row => row.ConsumedTotal += inner.Quantity.Amount,
			cancellationToken);
	}

	public Task HandleAsync(ShoppingItemRemovedIntegrationEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		return UpsertAsync(
			@event.OwnerId,
			inner.ItemName,
			inner.Quantity.Unit?.Value,
			row => row.RemovedTotal += inner.Quantity.Amount,
			cancellationToken);
	}

	public Task HandleAsync(ShoppingItemUndoBoughtIntegrationEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		return UpsertAsync(
			@event.OwnerId,
			inner.ItemName,
			inner.Quantity.Unit?.Value,
			row => row.BoughtTotal = Math.Max(0, row.BoughtTotal - inner.Quantity.Amount),
			cancellationToken);
	}

	public Task HandleAsync(ShoppingItemAddedAsAtHomeIntegrationEvent @event, CancellationToken cancellationToken = default)
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

	private async Task UpsertAsync(
		string ownerId,
		string itemName,
		string? unit,
		Action<IngredientBalanceReadItem> mutate,
		CancellationToken cancellationToken)
	{
		var nameKey = itemName.ToLowerInvariant();
		var row = await context.Set<IngredientBalanceReadItem>()
			.FirstOrDefaultAsync(
				r => r.OwnerId == ownerId
					&& r.NameKey == nameKey
					&& r.Unit == unit,
				cancellationToken);

		if (row is null)
		{
			var category = IngredientCategoryResolver.Resolve(itemName) ?? IngredientCategory.Other;
			row = new IngredientBalanceReadItem
			{
				Id = Guid.CreateVersion7(),
				OwnerId = ownerId,
				ItemName = itemName,
				NameKey = nameKey,
				Unit = unit,
				Category = category.Value,
			};
			context.Set<IngredientBalanceReadItem>().Add(row);
		}

		mutate(row);
		row.LastUpdated = timeProvider.GetUtcNow().UtcDateTime;
		await context.SaveChangesAsync(cancellationToken);
	}
}
