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

            // Title/description search via EF.Functions.ILike + EF.Property<string>
            // to bypass value object .Value access (which EF Core 10 cannot translate).
            var titleDescIds = await query
                .Where(r =>
                    EF.Functions.ILike(EF.Property<string>(r, "Title"), pattern) ||
                    EF.Functions.ILike(EF.Property<string>(r, "Description"), pattern))
                .Select(r => r.Id)
                .ToListAsync(cancellationToken);

            // Ingredient search via raw SQL (EF cannot translate value object access
            // inside OwnsMany subqueries even with EF.Property).
            var ingredientIds = await context.Database
                .SqlQuery<RecipeIdentifier>(
                    $"""
                    SELECT DISTINCT "RecipeId" AS "Value"
                    FROM "RecipeIngredients"
                    WHERE LOWER("Name") LIKE {pattern}
                    """)
                .ToListAsync(cancellationToken);

            var matchingIds = titleDescIds.Union(ingredientIds).ToHashSet();

            // Client-side ID filter — acceptable because the owner filter already
            // limits the result set to the current user's recipes.
            var ownerRecipes = await query.ToListAsync(cancellationToken);
            var filtered = ownerRecipes.Where(r => matchingIds.Contains(r.Id)).ToList();

            var sorted = ApplySortingInMemory(filtered, sorting);
            return (sorted.Skip(paging.Skip).Take(paging.PageSize.Value).ToList(), sorted.Count);
        }

        query = ApplySorting(query, sorting);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip(paging.Skip)
            .Take(paging.PageSize.Value)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
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
