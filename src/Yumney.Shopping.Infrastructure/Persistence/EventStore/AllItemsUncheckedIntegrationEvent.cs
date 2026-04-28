using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

#pragma warning disable SA1649
public sealed record AllItemsUncheckedIntegrationEvent(
	string OwnerId,
	Guid AggregateId,
	AllItemsUnchecked Inner) : ShoppingListIntegrationEvent(OwnerId, AggregateId);
