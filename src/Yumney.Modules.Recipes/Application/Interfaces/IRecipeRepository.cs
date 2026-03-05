using Yumney.Modules.Recipes.Domain.Recipe;

namespace Yumney.Modules.Recipes.Application.Interfaces;

public interface IRecipeRepository
{
    Task<Recipe?> FindByIdAsync(RecipeId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Recipe>> FindByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> ExistsWithUrlAsync(SourceUrl url, Guid userId, CancellationToken cancellationToken = default);

    Task AddAsync(Recipe recipe, CancellationToken cancellationToken = default);

    Task RemoveAsync(Recipe recipe, CancellationToken cancellationToken = default);
}
