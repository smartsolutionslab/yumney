using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Persistence;

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

		query = query
			.ApplySearch(search)
			.ApplyFilter(filter, GetFavoriteRecipeIdsOfUserQuery(owner))
			.ApplySorting(sorting);

		return await query
			.Include(recipe => recipe.Tags)
			.AsSplitQuery()
			.ToPagedListAsync(paging, cancellationToken);
	}

	private IQueryable<RecipeIdentifier> GetFavoriteRecipeIdsOfUserQuery(OwnerIdentifier owner)
	{
		var favoriteRecipeIds = context.RecipeFavorites
			.Where(favorite => favorite.Owner == owner)
			.Select(favorite => favorite.RecipeIdentifier);
		return favoriteRecipeIds;
	}
}
