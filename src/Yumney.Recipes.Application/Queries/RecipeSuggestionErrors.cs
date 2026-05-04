using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Queries;

public static class RecipeSuggestionErrors
{
	public static readonly ApiError SuggestionFailed = new("SUGGESTION_FAILED", "Recipe suggestion failed. Please try again.", 502);
	public static readonly ApiError NoIngredients = new("SUGGESTION_NO_INGREDIENTS", "No ingredients available — set up staples or buy items first.", 422);
}
