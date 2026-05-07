using SmartSolutionsLab.Yumney.Recipes.Client;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.ExternalServices;

public sealed class HttpRecipeIngredientLookup(IRecipesClient recipes) : IRecipeIngredientLookup
{
	public async Task<IReadOnlyList<RecipeIngredientLookupResult>> LookupAsync(
		RecipeReference recipe,
		CancellationToken cancellationToken = default)
	{
		var response = await recipes.GetRecipeAsync(recipe.Value, cancellationToken);
		if (response is null) return [];

		return response.Ingredients
			.Select(ingredient => new RecipeIngredientLookupResult(
				ingredient.Name,
				ingredient.Amount,
				ingredient.Unit,
				response.Servings))
			.ToList();
	}
}
