using SmartSolutionsLab.Yumney.Shared.Events;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

/// <summary>
/// Base record for all ShoppingList in-module bus envelopes. Carries the
/// routing pair (OwnerId, AggregateId) that identifies the list stream the
/// event came from. Concrete events stay as sealed records so the bus's
/// name-based handler discovery still works.
/// </summary>
public abstract record ShoppingListModuleEvent(string OwnerId, Guid AggregateId) : ModuleEvent(OwnerId);
