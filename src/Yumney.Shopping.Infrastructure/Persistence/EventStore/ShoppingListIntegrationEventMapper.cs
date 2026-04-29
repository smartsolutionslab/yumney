using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

/// <summary>
/// Maps a ShoppingList domain event to its in-module integration-event wrapper.
/// Shared by <see cref="EventStore.EfCoreShoppingListEventStore"/> /
/// <see cref="ShoppingUnitOfWork"/> at publish time and by the projection
/// rebuilder during replay.
/// </summary>
internal static class ShoppingListIntegrationEventMapper
{
	public static IIntegrationEvent? Map(string ownerId, Guid aggregateId, IDomainEvent domainEvent) => domainEvent switch
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
