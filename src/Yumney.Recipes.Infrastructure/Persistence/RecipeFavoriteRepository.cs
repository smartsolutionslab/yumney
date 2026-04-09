using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Domain.RecipeFavorite;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence;

public sealed class RecipeFavoriteRepository(RecipesDbContext context) : IRecipeFavoriteRepository
{
    public async Task<bool> IsFavoritedAsync(
        OwnerIdentifier owner,
        RecipeIdentifier recipeIdentifier,
        CancellationToken cancellationToken = default)
    {
        return await context.RecipeFavorites
            .AsNoTracking()
            .AnyAsync(
                f => f.Owner == owner && f.RecipeIdentifier == recipeIdentifier,
                cancellationToken);
    }

    public async Task<IReadOnlySet<Guid>> GetFavoritedIdsAsync(
        OwnerIdentifier owner,
        IReadOnlyCollection<RecipeIdentifier> recipeIdentifiers,
        CancellationToken cancellationToken = default)
    {
        if (recipeIdentifiers.Count == 0)
        {
            return new HashSet<Guid>();
        }

        var idList = recipeIdentifiers.ToList();
        var favorited = await context.RecipeFavorites
            .AsNoTracking()
            .Where(f => f.Owner == owner && idList.Contains(f.RecipeIdentifier))
            .Select(f => f.RecipeIdentifier)
            .ToListAsync(cancellationToken);

        return favorited.Select(r => r.Value).ToHashSet();
    }

    public async Task AddAsync(RecipeFavorite favorite, CancellationToken cancellationToken = default)
    {
        await context.RecipeFavorites.AddAsync(favorite, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(
        OwnerIdentifier owner,
        RecipeIdentifier recipeIdentifier,
        CancellationToken cancellationToken = default)
    {
        await context.RecipeFavorites
            .Where(f => f.Owner == owner && f.RecipeIdentifier == recipeIdentifier)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
