using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;

public sealed class ShoppingUnitOfWork(
	ShoppingDbContext context,
	IShoppingListRepository shoppingLists,
	IShoppingListEventStore listEventStore,
	IEventBus eventBus) : IShoppingUnitOfWork
{
	public IShoppingListRepository ShoppingLists => shoppingLists;

	public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		var trackedLists = context.ChangeTracker.Entries<ShoppingList>()
			.Where(e => e.State != EntityState.Detached)
			.Select(e => e.Entity)
			.Where(l => l.UncommittedEvents.Count > 0)
			.ToList();

		var pendingPublishes = trackedLists
			.Select(list => (list, events: list.UncommittedEvents.ToList()))
			.ToList();

		foreach (var list in trackedLists)
		{
			await listEventStore.AppendAsync(list, cancellationToken);
		}

		int result;
		try
		{
			result = await context.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException ex) when (ex.IsUniqueViolation())
		{
			throw new ConcurrencyConflictException(nameof(ShoppingList), trackedLists.FirstOrDefault()?.Identifier.Value ?? Guid.Empty, ex);
		}

		// Mark committed BEFORE publishing: events are already persisted to the store,
		// so the aggregate is conceptually "clean". A publish failure (e.g. a projection
		// handler throws) propagates to the caller, but the aggregate doesn't keep stale
		// uncommitted events that would re-stage with conflicting versions on retry.
		foreach (var (list, _) in pendingPublishes)
		{
			list.MarkCommitted();
		}

		foreach (var (list, events) in pendingPublishes)
		{
			foreach (var domainEvent in events)
			{
				var integrationEvent = MapToIntegrationEvent(list, domainEvent);
				if (integrationEvent is not null)
				{
					await eventBus.PublishAsync(integrationEvent, cancellationToken);
				}
			}
		}

		return result;
	}

	private static IIntegrationEvent? MapToIntegrationEvent(ShoppingList list, IDomainEvent domainEvent)
	{
		var ownerId = list.Owner.Value;
		var aggregateId = list.Identifier.Value;

		return domainEvent switch
		{
			ShoppingListCreated created => new ShoppingListCreatedIntegrationEvent(ownerId, aggregateId, created),
			ListItemAdded added => new ListItemAddedIntegrationEvent(ownerId, aggregateId, added),
			ListItemChecked checked_ => new ListItemCheckedIntegrationEvent(ownerId, aggregateId, checked_),
			ListItemUnchecked unchecked_ => new ListItemUncheckedIntegrationEvent(ownerId, aggregateId, unchecked_),
			AllItemsChecked allChecked => new AllItemsCheckedIntegrationEvent(ownerId, aggregateId, allChecked),
			AllItemsUnchecked allUnchecked => new AllItemsUncheckedIntegrationEvent(ownerId, aggregateId, allUnchecked),
			RecipeReferenceCleared cleared => new RecipeReferenceClearedIntegrationEvent(ownerId, aggregateId, cleared),
			_ => null,
		};
	}
}
