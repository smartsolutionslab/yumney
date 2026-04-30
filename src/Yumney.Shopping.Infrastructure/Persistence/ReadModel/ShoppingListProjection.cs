using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;

/// <summary>
/// Async projection handler — rebuilds the ShoppingList read model from
/// integration events published by the event store. Idempotent upserts so
/// replay produces the same final state.
/// </summary>
public sealed class ShoppingListProjection(ShoppingDbContext context)
	: IIntegrationEventHandler<ShoppingListCreatedIntegrationEvent>,
	  IIntegrationEventHandler<ListItemAddedIntegrationEvent>,
	  IIntegrationEventHandler<ListItemCheckedIntegrationEvent>,
	  IIntegrationEventHandler<ListItemUncheckedIntegrationEvent>,
	  IIntegrationEventHandler<AllItemsCheckedIntegrationEvent>,
	  IIntegrationEventHandler<AllItemsUncheckedIntegrationEvent>,
	  IIntegrationEventHandler<RecipeReferenceClearedIntegrationEvent>
{
	public async Task HandleAsync(ShoppingListCreatedIntegrationEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		var existing = await context.Set<ShoppingListSummaryReadItem>()
			.FirstOrDefaultAsync(s => s.Id == @event.AggregateId, cancellationToken);

		if (existing is null)
		{
			context.Set<ShoppingListSummaryReadItem>().Add(new ShoppingListSummaryReadItem
			{
				Id = @event.AggregateId,
				OwnerId = @event.OwnerId,
				Title = inner.Title.Value,
				RecipeIdentifier = inner.RecipeReference?.Value,
				ItemCount = 0,
				CreatedAt = inner.CreatedAt,
				LastUpdated = inner.CreatedAt,
			});
		}
		else
		{
			existing.OwnerId = @event.OwnerId;
			existing.Title = inner.Title.Value;
			existing.RecipeIdentifier = inner.RecipeReference?.Value;
			existing.CreatedAt = inner.CreatedAt;
			existing.LastUpdated = DateTime.UtcNow;
		}

		await context.SaveChangesAsync(cancellationToken);
	}

	public async Task HandleAsync(ListItemAddedIntegrationEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		var existing = await context.Set<ShoppingListItemReadItem>()
			.FirstOrDefaultAsync(i => i.Id == inner.ItemId.Value, cancellationToken);

		if (existing is null)
		{
			context.Set<ShoppingListItemReadItem>().Add(new ShoppingListItemReadItem
			{
				Id = inner.ItemId.Value,
				ListId = @event.AggregateId,
				OwnerId = @event.OwnerId,
				Name = inner.Name.Value,
				QuantityAmount = inner.Quantity?.Amount.Value,
				QuantityUnit = inner.Quantity?.Unit?.Value,
				IsChecked = false,
				CreatedAt = DateTime.UtcNow,
				LastUpdated = DateTime.UtcNow,
			});

			// Only increment when the item is newly inserted — otherwise replay/duplicate
			// delivery would drift the count while the items table stays at 1 row.
			await IncrementItemCountAsync(@event.AggregateId, cancellationToken);
		}

		await context.SaveChangesAsync(cancellationToken);
	}

	public async Task HandleAsync(ListItemCheckedIntegrationEvent @event, CancellationToken cancellationToken = default)
	{
		await SetCheckedAsync(@event.Inner.ItemId.Value, true, cancellationToken);
		await TouchSummaryAsync(@event.AggregateId, cancellationToken);
		await context.SaveChangesAsync(cancellationToken);
	}

	public async Task HandleAsync(ListItemUncheckedIntegrationEvent @event, CancellationToken cancellationToken = default)
	{
		await SetCheckedAsync(@event.Inner.ItemId.Value, false, cancellationToken);
		await TouchSummaryAsync(@event.AggregateId, cancellationToken);
		await context.SaveChangesAsync(cancellationToken);
	}

	public async Task HandleAsync(AllItemsCheckedIntegrationEvent @event, CancellationToken cancellationToken = default)
	{
		await SetAllCheckedAsync(@event.AggregateId, true, cancellationToken);
		await TouchSummaryAsync(@event.AggregateId, cancellationToken);
		await context.SaveChangesAsync(cancellationToken);
	}

	public async Task HandleAsync(AllItemsUncheckedIntegrationEvent @event, CancellationToken cancellationToken = default)
	{
		await SetAllCheckedAsync(@event.AggregateId, false, cancellationToken);
		await TouchSummaryAsync(@event.AggregateId, cancellationToken);
		await context.SaveChangesAsync(cancellationToken);
	}

	public async Task HandleAsync(RecipeReferenceClearedIntegrationEvent @event, CancellationToken cancellationToken = default)
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
		var items = await context.Set<ShoppingListItemReadItem>()
			.Where(item => item.ListId == listId)
			.ToListAsync(cancellationToken);
		foreach (var item in items)
		{
			item.IsChecked = isChecked;
			item.LastUpdated = DateTime.UtcNow;
		}
	}

	private async Task IncrementItemCountAsync(Guid listId, CancellationToken cancellationToken)
	{
		var summary = await context.Set<ShoppingListSummaryReadItem>()
			.FirstOrDefaultAsync(s => s.Id == listId, cancellationToken);
		if (summary is null) return;
		summary.ItemCount += 1;
		summary.LastUpdated = DateTime.UtcNow;
	}

	private async Task TouchSummaryAsync(Guid listId, CancellationToken cancellationToken)
	{
		var summary = await context.Set<ShoppingListSummaryReadItem>()
			.FirstOrDefaultAsync(s => s.Id == listId, cancellationToken);
		if (summary is null) return;
		summary.LastUpdated = DateTime.UtcNow;
	}
}
