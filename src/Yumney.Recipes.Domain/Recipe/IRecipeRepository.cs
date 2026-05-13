using SmartSolutionsLab.Yumney.Shared.Paging;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

public interface IRecipeRepository
{
	Task AddAsync(Recipe recipe, CancellationToken cancellationToken = default);

	Task<bool> ExistsBySourceUrlAsync(RecipeUrl sourceUrl, OwnerIdentifier owner, CancellationToken cancellationToken = default);

	// Read-only fetch — entity is not tracked. Throws EntityNotFoundException if not found.
	Task<Recipe> GetByIdAsync(RecipeIdentifier identifier, CancellationToken cancellationToken = default);

	// Tracked fetch for update / delete flows. Throws EntityNotFoundException if not found.
	Task<Recipe> GetByIdForUpdateAsync(RecipeIdentifier identifier, CancellationToken cancellationToken = default);

	Task<PagedResult<Recipe>> GetByOwnerAsync(
		OwnerIdentifier owner,
		PagingOptions paging,
		SortingOptions<RecipeSortField> sorting,
		SearchTerm? search = null,
		RecipeFilter? filter = null,
		CancellationToken cancellationToken = default);

	// Bulk fetch with ingredients eagerly loaded, ordered most-recent first.
	// Used by the recipe-matching engine ("What Can I Cook?"). Untracked.
	// `maxResults` caps the slice in SQL — required so callers can't trigger
	// an unbounded materialise on owners with thousands of recipes.
	Task<IReadOnlyList<Recipe>> GetRecentByOwnerWithIngredientsAsync(
		OwnerIdentifier owner,
		int maxResults,
		CancellationToken cancellationToken = default);

	void Remove(Recipe recipe);
}
