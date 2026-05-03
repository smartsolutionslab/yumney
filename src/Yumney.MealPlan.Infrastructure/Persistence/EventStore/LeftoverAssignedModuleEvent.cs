using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.Events;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.EventStore;

#pragma warning disable SA1649
public sealed record LeftoverAssignedModuleEvent(
	string OwnerId,
	string Week,
	LeftoverAssigned Inner) : MealPlanModuleEvent(OwnerId, Week);
