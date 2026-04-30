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
	}

	public async Task<Recipe> GetByIdAsync(RecipeIdentifier identifier, CancellationToken cancellationToken = default)
	{
		return await recipes
			.AsNoTracking()
			.Include(recipe => recipe.Ingredients)
			.Include(recipe => recipe.Steps.OrderBy(step => step.Number))
			.Include(recipe => recipe.Tags)
			.AsSplitQuery()
			.FirstOrDefaultAsync(recipe => recipe.Id == identifier, cancellationToken)
			?? throw new EntityNotFoundException(nameof(Recipe), identifier.Value);
	}

	public async Task<Recipe> GetByIdForUpdateAsync(RecipeIdentifier identifier, CancellationToken cancellationToken = default)
	{
		return await recipes
			.Include(recipe => recipe.Ingredients)
			.Include(recipe => recipe.Steps.OrderBy(step => step.Number))
			.Include(recipe => recipe.Tags)
			.AsSplitQuery()
			.FirstOrDefaultAsync(recipe => recipe.Id == identifier, cancellationToken)
			?? throw new EntityNotFoundException(nameof(Recipe), identifier.Value);
	}

	public async Task<bool> ExistsBySourceUrlAsync(RecipeUrl sourceUrl, OwnerIdentifier owner, CancellationToken cancellationToken = default)
	{
		return await recipes.AnyAsync(recipe => recipe.SourceUrl == sourceUrl && recipe.Owner == owner, cancellationToken);
	}

	public void Remove(Recipe recipe)
	{
		context.Recipes.Remove(recipe);
	}

	public async Task<IReadOnlyList<Recipe>> GetAllByOwnerWithIngredientsAsync(
		OwnerIdentifier owner,
		CancellationToken cancellationToken = default)
	{
		return await recipes
			.AsNoTracking()
			.Where(recipe => recipe.Owner == owner)
			.Include(recipe => recipe.Ingredients)
			.AsSplitQuery()
			.ToListAsync(cancellationToken);
	}

	public async Task<(IReadOnlyList<Recipe> Items, ItemCount TotalCount)> GetByOwnerAsync(
		OwnerIdentifier owner,
		PagingOptions paging,
		SortingOptions<RecipeSortField> sorting,
		SearchTerm? search = null,
		RecipeFilter? filter = null,
		CancellationToken cancellationToken = default)
	{
		var query = recipes.AsNoTracking().Where(recipe => recipe.Owner == owner);

		if (search is not null)
		{
			var pattern = $"%{search.Value}%";

			query = query.Where(recipe =>
				EF.Functions.ILike(recipe.Title, pattern) ||
				(recipe.Description != null && EF.Functions.ILike(recipe.Description, pattern)) ||
				recipe.Ingredients.Any(ingredient => EF.Functions.ILike(ingredient.Name, pattern)));
		}

		query = ApplyFilter(query, owner, filter);
		query = ApplySorting(query, sorting);

		var totalCount = await query.CountAsync(cancellationToken);
		var items = await query
			.Include(recipe => recipe.Tags)
			.AsSplitQuery()
			.Skip(paging.Skip)
			.Take(paging.PageSize.Value)
			.ToListAsync(cancellationToken);

		return (items, ItemCount.From(totalCount));
	}

	private static IQueryable<Recipe> ApplySorting(IQueryable<Recipe> query, SortingOptions<RecipeSortField> sorting)
	{
		return (sorting.SortBy, sorting.Direction) switch
		{
			(RecipeSortField.Name, SortDirection.Ascending) => query.OrderBy(recipe => recipe.Title),
			(RecipeSortField.Name, SortDirection.Descending) => query.OrderByDescending(recipe => recipe.Title),
			(RecipeSortField.Date, SortDirection.Ascending) => query.OrderBy(recipe => recipe.CreatedAt),
			(RecipeSortField.Date, SortDirection.Descending) => query.OrderByDescending(recipe => recipe.CreatedAt),
			_ => throw new InvalidOperationException($"Unsupported sort combination: {sorting.SortBy}, {sorting.Direction}"),
		};
	}

	private IQueryable<Recipe> ApplyFilter(IQueryable<Recipe> query, OwnerIdentifier owner, RecipeFilter? filter)
	{
		if (filter is null || filter.IsEmpty) return query;

		if (filter.Difficulty is not null)
		{
			query = query.Where(recipe => recipe.Difficulty == filter.Difficulty);
		}

		if (filter.MaxPrepTime is not null)
		{
			var maxPrep = filter.MaxPrepTime;
			query = query.Where(recipe => recipe.Timing != null && recipe.Timing.Preparation != null && recipe.Timing.Preparation <= maxPrep);
		}

		if (filter.MaxCookTime is not null)
		{
			var maxCook = filter.MaxCookTime;
			query = query.Where(recipe => recipe.Timing != null && recipe.Timing.Cooking != null && recipe.Timing.Cooking <= maxCook);
		}

		if (filter.Tags is not null && filter.Tags.Count > 0)
		{
			// AND logic: every requested tag must be present on the recipe.
			foreach (var requiredTag in filter.Tags)
			{
				var tagValue = requiredTag.Value;
				query = query.Where(recipe => recipe.Tags.Any(tag => tag.Value == tagValue));
			}
		}

		if (filter.FavoritesOnly == true)
		{
			var favoriteRecipeIds = context.RecipeFavorites
				.Where(favorite => favorite.Owner == owner)
				.Select(favorite => favorite.RecipeIdentifier);
			query = query.Where(recipe => favoriteRecipeIds.Contains(recipe.Id));
		}

		return query;
	}
}
