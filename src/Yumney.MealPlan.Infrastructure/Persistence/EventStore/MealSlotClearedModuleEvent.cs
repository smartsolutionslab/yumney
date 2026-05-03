using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.Events;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.EventStore;

#pragma warning disable SA1649
public sealed record MealSlotClearedModuleEvent(
	string OwnerId,
	string Week,
	MealSlotCleared Inner) : MealPlanModuleEvent(OwnerId, Week);
