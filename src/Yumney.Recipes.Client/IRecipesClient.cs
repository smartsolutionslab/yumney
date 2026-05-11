namespace SmartSolutionsLab.Yumney.Recipes.Client;

public interface IRecipesClient
{
	Task<RecipeResponse?> GetRecipeAsync(Guid recipeId, CancellationToken cancellationToken = default);

	Task<RecipeCatalogResponse> ListRecipeCatalogAsync(int pageSize, CancellationToken cancellationToken = default);
}

public sealed record RecipeResponse(int? Servings, IReadOnlyList<RecipeIngredientPayload> Ingredients);

public sealed record RecipeIngredientPayload(string Name, decimal? Amount, string? Unit);

public sealed record RecipeCatalogResponse(IReadOnlyList<RecipeCatalogItem> Items);

public sealed record RecipeCatalogItem(
	Guid Identifier,
	string Title,
	int? PrepTimeMinutes,
	int? CookTimeMinutes,
	string? Difficulty,
	IReadOnlyList<string> Tags,
	bool IsFavorite,
	int? Rating);
