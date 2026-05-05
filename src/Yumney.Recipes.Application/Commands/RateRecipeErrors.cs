using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands;

public static class RateRecipeErrors
{
	public static readonly ApiError AccessDenied = new(
		"RECIPE_ACCESS_DENIED",
		"Recipe not found.",
		404);
}
