using SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Client;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.ExternalServices;

public sealed class HttpRecipeTagsLookup(IRecipesClient recipes) : IRecipeTagsLookup
{
#pragma warning disable SA1303
	private const int catalogPageSize = 100;
#pragma warning restore SA1303

	public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> GetAllAsync(CancellationToken cancellationToken = default)
	{
		var response = await recipes.ListRecipeCatalogAsync(catalogPageSize, cancellationToken);
		return response.Items.ToDictionary(item => item.Identifier, item => item.Tags);
	}
}
