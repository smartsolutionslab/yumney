using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Recipes.Client;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.ExternalServices;

public sealed class HttpRecipeIngredientLookup(IRecipesClient recipes) : IRecipeIngredientLookup
{
	public async Task<IReadOnlyList<RecipeIngredientLookupResult>> LookupAsync(
		SlotRecipeIdentifier recipe,
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
