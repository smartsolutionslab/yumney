using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.Events;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.EventStore;

#pragma warning disable SA1649
public sealed record ExtendedModeEnabledIntegrationEvent(
	string OwnerId,
	string Week,
	ExtendedModeEnabled Inner) : MealPlanIntegrationEvent(OwnerId, Week);
