namespace Yumney.Recipes.Domain.Recipe;

public interface IRecipeRepository
{
    Task AddAsync(Recipe recipe, CancellationToken cancellationToken = default);

    Task<bool> ExistsBySourceUrlAsync(RecipeUrl sourceUrl, OwnerIdentifier owner, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Recipe> Items, int TotalCount)> GetByOwnerAsync(
        OwnerIdentifier owner,
        int skip,
        int take,
        RecipeSortField sortBy,
        SortDirection sortDirection,
        CancellationToken cancellationToken = default);
}
