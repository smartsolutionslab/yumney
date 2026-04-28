using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

#pragma warning disable SA1649
public sealed record ListItemAddedIntegrationEvent(
	string OwnerId,
	Guid AggregateId,
	ListItemAdded Inner) : ShoppingListIntegrationEvent(OwnerId, AggregateId);
