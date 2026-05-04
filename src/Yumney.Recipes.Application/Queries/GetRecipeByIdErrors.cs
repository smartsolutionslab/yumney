using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Queries;

public static class GetRecipeByIdErrors
{
	public static readonly ApiError NotFound = new("RECIPE_NOT_FOUND", "Recipe not found.", 404);
	public static readonly ApiError AccessDenied = new("RECIPE_ACCESS_DENIED", "Recipe not found.", 404);
}
