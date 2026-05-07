using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore.Events;

#pragma warning disable SA1649
public sealed record ListItemCheckedModuleEvent(
	string OwnerId,
	Guid AggregateId,
	ListItemChecked Inner) : ShoppingListModuleEvent(OwnerId, AggregateId);
