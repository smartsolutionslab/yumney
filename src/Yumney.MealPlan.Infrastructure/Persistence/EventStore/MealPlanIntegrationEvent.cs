using SmartSolutionsLab.Yumney.Shared.Events;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.EventStore;

/// <summary>
/// Base record for all MealPlan integration events. Carries the routing pair
/// (OwnerId, Week) that identifies the weekly plan stream the event came from.
/// Concrete events stay as sealed records so Wolverine's name-based handler
/// discovery still works.
/// </summary>
public abstract record MealPlanIntegrationEvent(string OwnerId, string Week) : IntegrationEvent;
