using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.Events;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.EventStore;

#pragma warning disable SA1649
public sealed record MealMarkedAsSkippedIntegrationEvent(
	string OwnerId,
	string Week,
	MealMarkedAsSkipped Inner) : MealPlanIntegrationEvent(OwnerId, Week);
