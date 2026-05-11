using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Queries;

public static class SuggestWeekPlanErrors
{
	public static readonly ApiError NoRecipes = new("mealplan.suggest.no_recipes", "Add at least one recipe before asking for a week suggestion.", 422);
	public static readonly ApiError SuggestionFailed = new("mealplan.suggest.failed", "Could not produce a suggested meal plan; please try again.", 500);
}
