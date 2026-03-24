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
            .AsSplitQuery()
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
            var pattern = $"%{search.Value.ToLowerInvariant()}%";

            // EF Core 10 + Npgsql cannot translate value object .Value access,
            // StringComparison overloads, or Contains on converted ID properties.
            // Use raw SQL for search, then filter the LINQ query by matching IDs.
            // Client-side ID filtering is acceptable because the owner filter
            // already limits the result set to the current user's recipes.
            var matchingIds = (await context.Database
                .SqlQuery<Guid>(
                    $"""
                    SELECT DISTINCT r."Id" AS "Value"
                    FROM "Recipes" r
                    LEFT JOIN "RecipeIngredients" ri ON ri."RecipeId" = r."Id"
                    WHERE LOWER(r."Title") LIKE {pattern}
                       OR LOWER(r."Description") LIKE {pattern}
                       OR LOWER(ri."Name") LIKE {pattern}
                    """)
                .ToListAsync(cancellationToken))
                .ToHashSet();

            // Load owner's recipes and filter client-side by search matches
            var ownerRecipes = await query.ToListAsync(cancellationToken);
            var filtered = ownerRecipes.Where(r => matchingIds.Contains(r.Id.Value)).ToList();

            var sorted = ApplySortingInMemory(filtered, sorting);
            var totalCount = sorted.Count;
            var items = sorted.Skip(paging.Skip).Take(paging.PageSize.Value).ToList();
            return (items, totalCount);
        }

        query = ApplySorting(query, sorting);

        var totalCountAll = await query.CountAsync(cancellationToken);

        var pagedItems = await query
            .Skip(paging.Skip)
            .Take(paging.PageSize.Value)
            .ToListAsync(cancellationToken);

        return (pagedItems, totalCountAll);
    }

    private static IQueryable<Recipe> ApplySorting(
        IQueryable<Recipe> query,
        SortingOptions<RecipeSortField> sorting)
    {
        return (sorting.SortBy, sorting.Direction) switch
        {
            (RecipeSortField.Name, SortDirection.Ascending) => query.OrderBy(r => r.Title),
            (RecipeSortField.Name, SortDirection.Descending) => query.OrderByDescending(r => r.Title),
            (RecipeSortField.Date, SortDirection.Ascending) => query.OrderBy(r => r.CreatedAt),
            (RecipeSortField.Date, SortDirection.Descending) => query.OrderByDescending(r => r.CreatedAt),
            _ => throw new InvalidOperationException(
                $"Unsupported sort combination: {sorting.SortBy}, {sorting.Direction}"),
        };
    }

    private static List<Recipe> ApplySortingInMemory(
        List<Recipe> recipes,
        SortingOptions<RecipeSortField> sorting)
    {
        return (sorting.SortBy, sorting.Direction) switch
        {
            (RecipeSortField.Name, SortDirection.Ascending) => [.. recipes.OrderBy(r => r.Title.Value)],
            (RecipeSortField.Name, SortDirection.Descending) => [.. recipes.OrderByDescending(r => r.Title.Value)],
            (RecipeSortField.Date, SortDirection.Ascending) => [.. recipes.OrderBy(r => r.CreatedAt)],
            (RecipeSortField.Date, SortDirection.Descending) => [.. recipes.OrderByDescending(r => r.CreatedAt)],
            _ => recipes,
        };
    }
}
