using System.Net.Http.Json;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.ExternalServices;

public sealed class HttpRecipeIngredientLookup(IHttpClientFactory httpClientFactory) : IRecipeIngredientLookup
{
	public async Task<IReadOnlyList<RecipeIngredientLookupResult>> LookupAsync(
		RecipeReference recipe,
		CancellationToken cancellationToken = default)
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
