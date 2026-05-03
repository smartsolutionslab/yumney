using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.Events;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.EventStore;

#pragma warning disable SA1649
public sealed record MealSetAsFreetextModuleEvent(
	string OwnerId,
	string Week,
	MealSetAsFreetext Inner) : MealPlanModuleEvent(OwnerId, Week);
