using SmartSolutionsLab.Yumney.Shared.Quantities;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;

public interface IIngredientCategoryService
{
	/// <summary>
	/// Categorize an ingredient. Uses static lookup first, then LLM for unknown items.
	/// </summary>
	/// <param name="itemName">The ingredient or item name.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The resolved category (never null — defaults to Other).</returns>
	Task<IngredientCategory> CategorizeAsync(string itemName, CancellationToken cancellationToken = default);
}
