namespace SmartSolutionsLab.Yumney.Recipes.Client;

public interface IRecipesClient
{
	Task<RecipeResponse?> GetRecipeAsync(Guid recipeId, CancellationToken cancellationToken = default);
}

public sealed record RecipeResponse(int? Servings, IReadOnlyList<RecipeIngredientPayload> Ingredients);

public sealed record RecipeIngredientPayload(string Name, decimal? Amount, string? Unit);
