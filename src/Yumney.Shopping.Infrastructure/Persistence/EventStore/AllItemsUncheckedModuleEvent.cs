using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

#pragma warning disable SA1649
public sealed record AllItemsUncheckedModuleEvent(
	string OwnerId,
	Guid AggregateId,
	AllItemsUnchecked Inner) : ShoppingListModuleEvent(OwnerId, AggregateId);
