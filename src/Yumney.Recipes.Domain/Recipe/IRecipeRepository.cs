using SmartSolutionsLab.Yumney.Shared.Abstractions;
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

	// Bulk fetch with ingredients eagerly loaded. Used by the recipe-matching
	// engine ("What Can I Cook?"). Untracked. Keep in mind this loads the full
	// recipe set for the owner — caller is responsible for any ranking + cap.
	Task<IReadOnlyList<Recipe>> GetAllByOwnerWithIngredientsAsync(
		OwnerIdentifier owner,
		CancellationToken cancellationToken = default);

	void Remove(Recipe recipe);
}
