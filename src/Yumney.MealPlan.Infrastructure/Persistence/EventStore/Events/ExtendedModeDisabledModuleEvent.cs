using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.Events;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.EventStore.Events;

#pragma warning disable SA1649
public sealed record ExtendedModeDisabledModuleEvent(
	string OwnerId,
	string Week,
	ExtendedModeDisabled Inner) : MealPlanModuleEvent(OwnerId, Week);
