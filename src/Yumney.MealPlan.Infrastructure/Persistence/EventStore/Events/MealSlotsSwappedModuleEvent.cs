using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.Events;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.EventStore.Events;

#pragma warning disable SA1649
public sealed record MealSlotsSwappedModuleEvent(
	string OwnerId,
	string Week,
	MealSlotsSwapped Inner) : MealPlanModuleEvent(OwnerId, Week);
