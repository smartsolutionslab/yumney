using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

namespace SmartSolutionsLab.Yumney.MealPlan.Api.Requests;

public sealed record AssignRecipeRequest(
	DayOfWeek Day,
	Guid RecipeIdentifier,
	string RecipeTitle,
	MealType MealType = MealType.Dinner,
	int? Servings = null);
