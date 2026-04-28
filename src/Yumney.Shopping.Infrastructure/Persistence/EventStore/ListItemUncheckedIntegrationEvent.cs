using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

#pragma warning disable SA1649
public sealed record ListItemUncheckedIntegrationEvent(
	string OwnerId,
	Guid AggregateId,
	ListItemUnchecked Inner) : ShoppingListIntegrationEvent(OwnerId, AggregateId);
