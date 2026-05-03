using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

#pragma warning disable SA1649
public sealed record ListItemAddedModuleEvent(
	string OwnerId,
	Guid AggregateId,
	ListItemAdded Inner) : ShoppingListModuleEvent(OwnerId, AggregateId);
