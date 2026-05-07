using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore.Events;

#pragma warning disable SA1649
public sealed record AllItemsCheckedModuleEvent(
	string OwnerId,
	Guid AggregateId,
	AllItemsChecked Inner) : ShoppingListModuleEvent(OwnerId, AggregateId);
