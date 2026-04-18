using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Commands;

public static class GenerateShoppingListErrors
{
	public static readonly ApiError NoPlanFound = new("MEALPLAN_NOT_FOUND", "No meal plan found for this week.", 404);
	public static readonly ApiError NoRecipes = new("MEALPLAN_NO_RECIPES", "No recipes assigned to this week's plan.", 400);
}
