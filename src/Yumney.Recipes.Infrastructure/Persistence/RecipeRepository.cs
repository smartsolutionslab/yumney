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

    public async Task<Recipe?> GetByIdAsync(Guid identifier, CancellationToken cancellationToken = default)
    {
        return await context.Recipes
            .Include(r => r.Ingredients)
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == identifier, cancellationToken);
    }

    public async Task<bool> ExistsBySourceUrlAsync(RecipeUrl sourceUrl, OwnerIdentifier owner, CancellationToken cancellationToken = default)
    {
        return await context.Recipes.AnyAsync(
            r => r.SourceUrl == sourceUrl && r.Owner == owner,
            cancellationToken);
    }

    public async Task<(IReadOnlyList<Recipe> Items, int TotalCount)> GetByOwnerAsync(
        OwnerIdentifier owner,
        int skip,
        int take,
        RecipeSortField sortBy,
        SortDirection sortDirection,
        CancellationToken cancellationToken = default)
    {
        var query = context.Recipes.Where(r => r.Owner == owner);

        query = (sortBy, sortDirection) switch
        {
            (RecipeSortField.Name, SortDirection.Ascending) => query.OrderBy(r => r.Title),
            (RecipeSortField.Name, SortDirection.Descending) => query.OrderByDescending(r => r.Title),
            (RecipeSortField.Date, SortDirection.Ascending) => query.OrderBy(r => r.CreatedAt),
            (RecipeSortField.Date, SortDirection.Descending) => query.OrderByDescending(r => r.CreatedAt),
            _ => throw new InvalidOperationException(
                $"Unsupported sort combination: {sortBy}, {sortDirection}"),
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
