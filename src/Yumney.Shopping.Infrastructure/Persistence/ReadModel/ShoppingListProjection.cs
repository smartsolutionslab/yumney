using Microsoft.EntityFrameworkCore;
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
		var existing = await context.Set<ShoppingListSummaryReadItem>()
			.FirstOrDefaultAsync(s => s.Id == @event.AggregateId, cancellationToken);

		// Out-of-order delivery: ListItemAdded module events publish on a separate
		// RabbitMQ queue from ShoppingListCreated and can arrive first. Their
		// atomic-increment ExecuteUpdate then matches zero rows (no summary yet).
		// Reconcile here by counting the already-projected items table so the
		// initial ItemCount reflects items that landed before this handler ran.
		var itemCount = await context.Set<ShoppingListItemReadItem>()
			.CountAsync(item => item.ListId == @event.AggregateId, cancellationToken);

		if (existing is null)
		{
			var item = new ShoppingListSummaryReadItem
			{
				Id = @event.AggregateId,
				OwnerId = @event.OwnerId,
				Title = inner.Title.Value,
				RecipeIdentifier = inner.RecipeReference?.Value,
				ItemCount = itemCount,
				CreatedAt = inner.CreatedAt,
				LastUpdated = inner.CreatedAt,
			};
			context.Set<ShoppingListSummaryReadItem>().Add(item);
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

		// Atomic increment via SQL — Wolverine processes messages on a queue with
		// MaxDegreeOfParallelism = max(processor-count, 5), so two concurrent
		// ListItemAdded events for the same list would race a read-modify-write
		// on the summary row. ExecuteUpdate emits "SET ItemCount = ItemCount + 1"
		// which Postgres re-evaluates against the latest committed row under the
		// row-level UPDATE lock; both increments land instead of stomping each
		// other. Updates 0 rows if the summary hasn't been projected yet — that
		// path is handled by ShoppingListCreated handler reconciling against the
		// items table when it runs later.
		var now = DateTime.UtcNow;
		await context.Set<ShoppingListSummaryReadItem>()
			.Where(summary => summary.Id == @event.AggregateId)
			.ExecuteUpdateAsync(
				setters => setters
					.SetProperty(summary => summary.ItemCount, summary => summary.ItemCount + 1)
					.SetProperty(summary => summary.LastUpdated, _ => now),
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
