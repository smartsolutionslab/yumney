using System.Net.Http.Json;
using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Services;

public sealed class HttpRecipeIngredientProvider(IHttpClientFactory httpClientFactory) : IRecipeIngredientProvider
{
    public async Task<IReadOnlyList<RecipeIngredientInfo>> GetIngredientsAsync(
        Guid recipeIdentifier,
        CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient("recipes-api");
        var url = $"/api/v1/recipes/{recipeIdentifier}";
        var response = await client.GetFromJsonAsync<RecipeResponse>(url, cancellationToken);

        if (response is null) return [];

        return response.Ingredients
            .Select(i => new RecipeIngredientInfo(i.Name, i.Amount, i.Unit, response.Servings))
            .ToList();
    }

    private sealed record RecipeResponse(int? Servings, IReadOnlyList<IngredientResponse> Ingredients);

    private sealed record IngredientResponse(string Name, decimal? Amount, string? Unit);
}
