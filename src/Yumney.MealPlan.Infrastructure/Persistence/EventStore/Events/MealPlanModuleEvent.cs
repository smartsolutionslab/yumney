using SmartSolutionsLab.Yumney.Shared.Events;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.EventStore.Events;

/// <summary>
/// Base record for all MealPlan in-module bus envelopes. Carries the routing
/// pair (OwnerId, Week) that identifies the weekly plan stream the event came
/// from. Concrete events stay as sealed records so Wolverine's name-based
/// handler discovery still works.
/// </summary>
public abstract record MealPlanModuleEvent(string OwnerId, string Week) : ModuleEvent(OwnerId);
