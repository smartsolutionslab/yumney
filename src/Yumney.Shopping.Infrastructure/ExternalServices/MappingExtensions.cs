using SmartSolutionsLab.Yumney.Recipes.Client;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.ExternalServices;

public static class MappingExtensions
{
	public static RecipeIngredientLookupResult ToResult(this RecipeIngredientPayload ingredient, RecipeResponse response)
	{
		return new RecipeIngredientLookupResult(
			ingredient.Name,
			ingredient.Amount,
			ingredient.Unit,
			response.Servings);
	}
}
