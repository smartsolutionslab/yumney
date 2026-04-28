using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.Events;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.EventStore;

#pragma warning disable SA1649
public sealed record MealResetToPlannedIntegrationEvent(
	string OwnerId,
	string Week,
	MealResetToPlanned Inner) : MealPlanIntegrationEvent(OwnerId, Week);
