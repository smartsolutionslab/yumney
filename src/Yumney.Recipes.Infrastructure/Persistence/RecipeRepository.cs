using Microsoft.EntityFrameworkCore;
using Yumney.Recipes.Domain.Recipe;

namespace Yumney.Recipes.Infrastructure.Persistence;

public sealed class RecipeRepository(RecipesDbContext context) : IRecipeRepository
{
    public async Task AddAsync(Recipe recipe, CancellationToken cancellationToken = default)
    {
        await context.Recipes.AddAsync(recipe, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsBySourceUrlAsync(RecipeUrl sourceUrl, OwnerIdentifier owner, CancellationToken cancellationToken = default)
    {
        return await context.Recipes.AnyAsync(
            r => r.SourceUrl == sourceUrl && r.Owner == owner,
            cancellationToken);
    }
}
