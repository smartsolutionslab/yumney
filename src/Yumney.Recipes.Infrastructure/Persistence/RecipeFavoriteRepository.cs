using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Domain.RecipeFavorite;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence;

public sealed class RecipeFavoriteRepository(RecipesDbContext context) : IRecipeFavoriteRepository
{
    private readonly DbSet<RecipeFavorite> favorites = context.RecipeFavorites;

    public async Task<bool> IsFavoritedAsync(OwnerIdentifier owner, RecipeIdentifier recipeIdentifier, CancellationToken cancellationToken = default)
    {
        return await favorites
            .AsNoTracking()
            .AnyAsync(f => f.Owner == owner && f.RecipeIdentifier == recipeIdentifier, cancellationToken);
    }

    public async Task<IReadOnlySet<Guid>> GetFavoritedIdsAsync(
        OwnerIdentifier owner,
        IReadOnlyCollection<RecipeIdentifier> recipeIdentifiers,
        CancellationToken cancellationToken = default)
    {
        if (recipeIdentifiers.Count == 0) return new HashSet<Guid>();

        var idList = recipeIdentifiers.ToList();
        var favorited = await favorites
            .AsNoTracking()
            .Where(f => f.Owner == owner && idList.Contains(f.RecipeIdentifier))
            .Select(f => f.RecipeIdentifier)
            .ToListAsync(cancellationToken);

        return favorited.Select(r => r.Value).ToHashSet();
    }

    public async Task AddAsync(RecipeFavorite favorite, CancellationToken cancellationToken = default)
    {
        await favorites.AddAsync(favorite, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(OwnerIdentifier owner, RecipeIdentifier recipeIdentifier, CancellationToken cancellationToken = default)
    {
        await favorites
            .Where(f => f.Owner == owner && f.RecipeIdentifier == recipeIdentifier)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
