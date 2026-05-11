using SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Client;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.ExternalServices;

public sealed class HttpRecipeCatalogProvider(IRecipesClient recipes) : IRecipeCatalogProvider
{
	public async Task<IReadOnlyList<RecipeCatalogEntry>> ListAsync(int pageSize, CancellationToken cancellationToken = default)
	{
		var response = await recipes.ListRecipeCatalogAsync(pageSize, cancellationToken);
		return response.Items
			.Select(item => new RecipeCatalogEntry(
				item.Identifier,
				item.Title,
				item.PrepTimeMinutes,
				item.CookTimeMinutes,
				item.Difficulty,
				item.Tags,
				item.IsFavorite,
				item.Rating))
			.ToList();
	}
}
