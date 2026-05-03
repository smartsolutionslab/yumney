namespace SmartSolutionsLab.Yumney.Shopping.Application.DTOs;

public sealed record RecipeIngredientLookupResult(
	string Name,
	decimal? Amount,
	string? Unit,
	int? RecipeServings);
