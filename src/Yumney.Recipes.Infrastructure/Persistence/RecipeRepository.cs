using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence;

public sealed class RecipeRepository(RecipesDbContext context) : IRecipeRepository
{
    private readonly DbSet<Recipe> recipes = context.Recipes;

    public async Task AddAsync(Recipe recipe, CancellationToken cancellationToken = default)
    {
        await recipes.AddAsync(recipe, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Recipe?> GetByIdAsync(RecipeIdentifier identifier, CancellationToken cancellationToken = default)
    {
        return await recipes
            .Include(r => r.Ingredients)
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == identifier, cancellationToken);
    }

    public async Task<bool> ExistsBySourceUrlAsync(RecipeUrl sourceUrl, OwnerIdentifier owner, CancellationToken cancellationToken = default)
    {
        return await recipes.AnyAsync(r => r.SourceUrl == sourceUrl && r.Owner == owner, cancellationToken);
    }

    public async Task UpdateAsync(Recipe recipe, CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Recipe recipe, CancellationToken cancellationToken = default)
    {
        context.Recipes.Remove(recipe);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Recipe> Items, int TotalCount)> GetByOwnerAsync(
        OwnerIdentifier owner,
        PagingOptions paging,
        SortingOptions<RecipeSortField> sorting,
        SearchTerm? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = recipes.AsNoTracking().Where(r => r.Owner == owner);

        if (search is not null)
        {
            var term = search.Value.ToLowerInvariant();

            // Ingredient search runs as a separate query because EF Core cannot translate
            // value object .Value access inside OwnsMany subqueries.
            var ingredientMatchIds = await context.Database
                .SqlQuery<Guid>(
                    $"""SELECT DISTINCT "RecipeId" AS "Value" FROM "RecipeIngredients" WHERE LOWER("Name") LIKE {'%' + term + '%'}""")
                .ToListAsync(cancellationToken);

#pragma warning disable CA1862 // EF Core translates ToLowerInvariant().Contains() to SQL LOWER() LIKE — StringComparison overload is not translatable
            query = query.Where(r =>
                r.Title.Value.ToLowerInvariant().Contains(term) ||
                (r.Description != null && r.Description.Value.ToLowerInvariant().Contains(term)) ||
                ingredientMatchIds.Contains(r.Id.Value));
#pragma warning restore CA1862
        }

        query = (sorting.SortBy, sorting.Direction) switch
        {
            (RecipeSortField.Name, SortDirection.Ascending) => query.OrderBy(r => r.Title),
            (RecipeSortField.Name, SortDirection.Descending) => query.OrderByDescending(r => r.Title),
            (RecipeSortField.Date, SortDirection.Ascending) => query.OrderBy(r => r.CreatedAt),
            (RecipeSortField.Date, SortDirection.Descending) => query.OrderByDescending(r => r.CreatedAt),
            _ => throw new InvalidOperationException($"Unsupported sort combination: {sorting.SortBy}, {sorting.Direction}"),
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip(paging.Skip)
            .Take(paging.PageSize.Value)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
