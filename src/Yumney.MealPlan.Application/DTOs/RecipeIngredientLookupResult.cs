namespace SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;

public sealed record RecipeIngredientLookupResult(
	string Name,
	decimal? Amount,
	string? Unit,
	int? RecipeServings);
