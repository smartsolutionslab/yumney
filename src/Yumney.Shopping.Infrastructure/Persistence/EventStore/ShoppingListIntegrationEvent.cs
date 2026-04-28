using SmartSolutionsLab.Yumney.Shared.Events;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

/// <summary>
/// Base record for all ShoppingList integration events. Carries the routing pair
/// (OwnerId, AggregateId) that identifies the list stream the event came from.
/// Concrete events stay as sealed records so the bus's name-based handler
/// discovery still works.
/// </summary>
public abstract record ShoppingListIntegrationEvent(string OwnerId, Guid AggregateId) : IntegrationEvent;
