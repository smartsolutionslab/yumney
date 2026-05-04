using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Services;

/// <summary>
/// Deterministic categorizer for E2E tests — relies entirely on the static
/// resolver and falls back to <see cref="IngredientCategory.Other"/> for
/// unknown items, so no LLM is required during automated runs.
/// </summary>
public sealed class StubShoppingItemCategorizer : IShoppingItemCategorizer
{
	public Task<IngredientCategory> CategorizeAsync(ItemName name, CancellationToken cancellationToken = default)
	{
		var category = IngredientCategoryResolver.Resolve(name.Value) ?? IngredientCategory.Other;
		return Task.FromResult(category);
	}

	public Task<IReadOnlyDictionary<ItemName, IngredientCategory>> CategorizeManyAsync(
		IReadOnlyCollection<ItemName> names,
		CancellationToken cancellationToken = default)
	{
		IReadOnlyDictionary<ItemName, IngredientCategory> map = names
			.DistinctBy(n => n.Value)
			.ToDictionary(name => name, name => IngredientCategoryResolver.Resolve(name.Value) ?? IngredientCategory.Other);
		return Task.FromResult(map);
	}
}
