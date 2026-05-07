using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore.Events;

#pragma warning disable SA1649
public sealed record ListItemCategoryChangedModuleEvent(
	string OwnerId,
	Guid AggregateId,
	ListItemCategoryChanged Inner) : ShoppingListModuleEvent(OwnerId, AggregateId);
