using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.CrossModule;
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
				var integrationEvent = ShoppingListIntegrationEventMapper.Map(
					list.Owner.Value, list.Identifier.Value, domainEvent);
				if (integrationEvent is not null)
				{
					await eventBus.PublishAsync(integrationEvent, cancellationToken);
				}

				var crossModuleEvent = MapToCrossModuleEvent(list, domainEvent);
				if (crossModuleEvent is not null)
				{
					await eventBus.PublishAsync(crossModuleEvent, cancellationToken);
				}
			}
		}

		return result;
	}

	private static IIntegrationEvent? MapToCrossModuleEvent(ShoppingList list, IDomainEvent domainEvent)
	{
		var ownerId = list.Owner.Value;
		var aggregateId = list.Identifier.Value;

		return domainEvent switch
		{
			ShoppingListCreated created => new ShoppingListCreatedCrossModuleIntegrationEvent(
				ownerId,
				aggregateId,
				created.Title.Value,
				created.RecipeReference?.Value,
				created.CreatedAt),
			RecipeReferenceCleared => new ShoppingListRecipeReferenceClearedCrossModuleIntegrationEvent(
				ownerId,
				aggregateId),
			_ => null,
		};
	}
}
