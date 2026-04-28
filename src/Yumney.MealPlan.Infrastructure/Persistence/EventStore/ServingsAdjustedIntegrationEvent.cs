using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.Events;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.EventStore;

#pragma warning disable SA1649
public sealed record ServingsAdjustedIntegrationEvent(
	string OwnerId,
	string Week,
	ServingsAdjusted Inner) : MealPlanIntegrationEvent(OwnerId, Week);
