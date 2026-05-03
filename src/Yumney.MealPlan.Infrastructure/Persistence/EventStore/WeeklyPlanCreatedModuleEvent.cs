using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.Events;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.EventStore;

#pragma warning disable SA1649
public sealed record WeeklyPlanCreatedModuleEvent(
	string OwnerId,
	string Week,
	WeeklyPlanCreated Inner) : MealPlanModuleEvent(OwnerId, Week);
