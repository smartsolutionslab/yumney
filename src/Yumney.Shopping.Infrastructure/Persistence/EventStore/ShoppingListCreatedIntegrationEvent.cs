using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

#pragma warning disable SA1649
public sealed record ShoppingListCreatedIntegrationEvent(
	string OwnerId,
	Guid AggregateId,
	ShoppingListCreated Inner) : ShoppingListIntegrationEvent(OwnerId, AggregateId);
