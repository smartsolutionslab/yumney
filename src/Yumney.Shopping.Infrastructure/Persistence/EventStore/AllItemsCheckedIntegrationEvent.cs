using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

#pragma warning disable SA1649
public sealed record AllItemsCheckedIntegrationEvent(
	string OwnerId,
	Guid AggregateId,
	AllItemsChecked Inner) : ShoppingListIntegrationEvent(OwnerId, AggregateId);
