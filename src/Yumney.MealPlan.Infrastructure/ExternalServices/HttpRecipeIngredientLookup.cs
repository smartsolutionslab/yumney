using System.Net.Http.Json;
using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.ExternalServices;

public sealed class HttpRecipeIngredientLookup(IHttpClientFactory httpClientFactory) : IRecipeIngredientLookup
{
	public async Task<IReadOnlyList<RecipeIngredientLookupResult>> LookupAsync(SlotRecipeIdentifier recipe, CancellationToken cancellationToken = default)
	{
		var client = httpClientFactory.CreateClient("recipes-api");
		var url = $"/api/v1/recipes/{recipe.Value}";
		var response = await client.GetFromJsonAsync<RecipeWireResponse>(url, cancellationToken);

		if (response is null) return [];

		return response.Ingredients
			.Select(ingredient => new RecipeIngredientLookupResult(
				ingredient.Name,
				ingredient.Amount,
				ingredient.Unit,
				response.Servings))
			.ToList();
	}

	private sealed record RecipeWireResponse(int? Servings, IReadOnlyList<IngredientWireResponse> Ingredients);

	private sealed record IngredientWireResponse(string Name, decimal? Amount, string? Unit);
}
