using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.Events;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.EventStore;

#pragma warning disable SA1649
public sealed record MealMarkedAsCookedModuleEvent(
	string OwnerId,
	string Week,
	MealMarkedAsCooked Inner) : MealPlanModuleEvent(OwnerId, Week);
