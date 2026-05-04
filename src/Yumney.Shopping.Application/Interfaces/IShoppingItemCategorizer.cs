using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;

/// <summary>
/// Categorizes shopping list items into store aisle groups (US-083).
/// Implementations should attempt a static lookup first and fall back to
/// an LLM call for unknown items, never returning null.
/// </summary>
public interface IShoppingItemCategorizer
{
	Task<IngredientCategory> CategorizeAsync(ItemName name, CancellationToken cancellationToken = default);

	Task<IReadOnlyDictionary<ItemName, IngredientCategory>> CategorizeManyAsync(
		IReadOnlyCollection<ItemName> names,
		CancellationToken cancellationToken = default);
}
