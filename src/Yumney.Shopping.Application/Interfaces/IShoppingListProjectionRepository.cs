using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;

/// <summary>
/// Read-side query repository backed by the ShoppingList projection tables
/// (<c>ShoppingListSummaryReadItems</c>, <c>ShoppingListItemReadItems</c>).
/// Phase 4 cuts the list-detail / list-collection queries over from the
/// relational write tables to this repository.
/// </summary>
public interface IShoppingListProjectionRepository
{
	Task<(IReadOnlyList<ShoppingListSummary> Items, ItemCount TotalCount)> GetByOwnerAsync(
		OwnerIdentifier owner,
		PagingOptions paging,
		SortingOptions<ShoppingListSortField> sorting,
		CancellationToken cancellationToken = default);

	Task<ShoppingListProjectedDetail> GetByIdAsync(ShoppingListIdentifier identifier, CancellationToken cancellationToken = default);

	Task<IReadOnlyList<ShoppingListIdentifier>> FindIdsByRecipeAsync(
		OwnerIdentifier owner,
		RecipeReference recipeReference,
		CancellationToken cancellationToken = default);
}
