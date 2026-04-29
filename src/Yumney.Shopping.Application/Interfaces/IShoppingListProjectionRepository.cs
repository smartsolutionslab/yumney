using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
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

	/// <summary>
	/// Loads the projection-shaped detail for one list. Throws
	/// <see cref="EntityNotFoundException"/> if no projection row exists.
	/// </summary>
	/// <param name="identifier">The list identifier.</param>
	/// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
	/// <returns>The owner-tagged DTO for the list.</returns>
	Task<ShoppingListProjectedDetail> GetByIdAsync(
		ShoppingListIdentifier identifier,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns the identifiers of every list owned by <paramref name="owner"/> whose
	/// summary row references the given recipe. Used by integration handlers (e.g.
	/// recipe deletion) to find the aggregates that need a follow-up command.
	/// </summary>
	/// <param name="owner">List owner.</param>
	/// <param name="recipeReference">Recipe pointer to match.</param>
	/// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
	/// <returns>List identifiers, possibly empty.</returns>
	Task<IReadOnlyList<ShoppingListIdentifier>> FindIdsByRecipeAsync(
		OwnerIdentifier owner,
		RecipeReference recipeReference,
		CancellationToken cancellationToken = default);
}

/// <summary>
/// Projection-shaped result of <see cref="IShoppingListProjectionRepository.GetByIdAsync"/>.
/// Carries the owner separately so the query handler can apply access control
/// without rebuilding the aggregate.
/// </summary>
public sealed record ShoppingListProjectedDetail(string OwnerId, ShoppingListDetailDto Dto);
