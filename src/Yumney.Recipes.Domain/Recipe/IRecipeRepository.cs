namespace Yumney.Recipes.Domain.Recipe;

public interface IRecipeRepository
{
    Task AddAsync(Recipe recipe, CancellationToken cancellationToken = default);

    Task<bool> ExistsBySourceUrlAsync(RecipeUrl sourceUrl, OwnerIdentifier owner, CancellationToken cancellationToken = default);
}
