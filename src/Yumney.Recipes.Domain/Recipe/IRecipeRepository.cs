using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

public interface IRecipeRepository
{
    Task AddAsync(Recipe recipe, CancellationToken cancellationToken = default);

    Task<bool> ExistsBySourceUrlAsync(RecipeUrl sourceUrl, OwnerIdentifier owner, CancellationToken cancellationToken = default);

    // Read-only fetch — entity is not tracked.
    Task<Recipe?> GetByIdAsync(RecipeIdentifier identifier, CancellationToken cancellationToken = default);

    // Tracked fetch for update / delete flows.
    Task<Recipe?> GetByIdForUpdateAsync(RecipeIdentifier identifier, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Recipe> Items, ItemCount TotalCount)> GetByOwnerAsync(
        OwnerIdentifier owner,
        PagingOptions paging,
        SortingOptions<RecipeSortField> sorting,
        SearchTerm? search = null,
        RecipeFilter? filter = null,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(Recipe recipe, CancellationToken cancellationToken = default);

    Task DeleteAsync(Recipe recipe, CancellationToken cancellationToken = default);
}
