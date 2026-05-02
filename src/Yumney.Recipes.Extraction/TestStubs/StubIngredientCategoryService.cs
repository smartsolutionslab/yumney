using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.TestStubs;

internal sealed class StubIngredientCategoryService : IIngredientCategoryService
{
	public Task<IngredientCategory> CategorizeAsync(string itemName, CancellationToken cancellationToken = default) =>
		Task.FromResult(IngredientCategory.Other);
}
