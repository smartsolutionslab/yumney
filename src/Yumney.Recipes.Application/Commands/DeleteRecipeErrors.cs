using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands;

public static class DeleteRecipeErrors
{
	public static readonly ApiError NotFound = new("RECIPE_NOT_FOUND", "Recipe not found.", 404);
	public static readonly ApiError AccessDenied = new("RECIPE_ACCESS_DENIED", "Recipe not found.", 404);
}
