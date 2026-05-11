using SmartSolutionsLab.Yumney.Shared.Web;

namespace SmartSolutionsLab.Yumney.Recipes.Client;

internal sealed class RecipesClient(IModuleHttpClientFactory factory) : IRecipesClient
{
	private readonly IModuleHttpClient http = factory.For("recipes-api");

	public Task<RecipeResponse?> GetRecipeAsync(Guid recipeId, CancellationToken cancellationToken = default) =>
		http.FindAsync<RecipeResponse>(
			$"/api/v1/recipes/{recipeId}",
			"GetRecipe",
			cancellationToken);

	public Task<RecipeCatalogResponse> ListRecipeCatalogAsync(int pageSize, CancellationToken cancellationToken = default) =>
		http.GetOrDefaultAsync(
			$"/api/v1/recipes?page=1&pageSize={pageSize}&sortBy=Date&sortDirection=Descending",
			new RecipeCatalogResponse([]),
			"ListRecipeCatalog",
			cancellationToken);
}
