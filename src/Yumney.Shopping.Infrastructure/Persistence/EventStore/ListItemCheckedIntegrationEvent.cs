using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

#pragma warning disable SA1649
public sealed record ListItemCheckedIntegrationEvent(
	string OwnerId,
	Guid AggregateId,
	ListItemChecked Inner) : ShoppingListIntegrationEvent(OwnerId, AggregateId);
