using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore.Events;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

/// <summary>
/// Maps a ShoppingList domain event to its in-module bus envelope. Shared by
/// <see cref="ShoppingListEventStore"/> at publish time and by the
/// projection rebuilder during replay.
/// </summary>
internal static class ShoppingListModuleEventMapper
{
	public static IModuleEvent? Map(string ownerId, Guid aggregateId, IDomainEvent domainEvent) => domainEvent switch
	{
		ShoppingListCreated created => new ShoppingListCreatedModuleEvent(ownerId, aggregateId, created),
		ListItemAdded added => new ListItemAddedModuleEvent(ownerId, aggregateId, added),
		ListItemChecked checked_ => new ListItemCheckedModuleEvent(ownerId, aggregateId, checked_),
		ListItemUnchecked unchecked_ => new ListItemUncheckedModuleEvent(ownerId, aggregateId, unchecked_),
		ListItemCategoryChanged categoryChanged => new ListItemCategoryChangedModuleEvent(ownerId, aggregateId, categoryChanged),
		AllItemsChecked allChecked => new AllItemsCheckedModuleEvent(ownerId, aggregateId, allChecked),
		AllItemsUnchecked allUnchecked => new AllItemsUncheckedModuleEvent(ownerId, aggregateId, allUnchecked),
		RecipeReferenceCleared cleared => new RecipeReferenceClearedModuleEvent(ownerId, aggregateId, cleared),
		_ => null,
	};
}
