using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.Events;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.EventStore;

#pragma warning disable SA1649
public sealed record RecipeAssignedIntegrationEvent(
	string OwnerId,
	string Week,
	RecipeAssigned Inner) : MealPlanIntegrationEvent(OwnerId, Week);
