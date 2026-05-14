using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Domain.RecipeFavorite;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence;

public sealed class RecipeFavoriteRepository(RecipesDbContext context) : IRecipeFavoriteRepository
{
	private readonly DbSet<RecipeFavorite> favorites = context.RecipeFavorites;

	public async Task<bool> IsFavoritedAsync(
		OwnerIdentifier owner,
		RecipeIdentifier recipe,
		CancellationToken cancellationToken = default)
	{
		return await favorites
			.AsNoTracking()
			.AnyAsync(
				favorite => favorite.Owner == owner && favorite.Recipe == recipe,
				cancellationToken);
	}

	public async Task<IReadOnlySet<Guid>> GetFavoritedIdsAsync(
		OwnerIdentifier owner,
		IReadOnlyCollection<RecipeIdentifier> recipes,
		CancellationToken cancellationToken = default)
	{
		if (recipes.Count == 0) return new HashSet<Guid>();

		var idList = recipes.ToList();
		var favorited = await favorites
			.AsNoTracking()
			.Where(favorite => favorite.Owner == owner && idList.Contains(favorite.Recipe))
			.Select(favorite => favorite.Recipe)
			.ToListAsync(cancellationToken);

		return favorited.Select(recipeId => recipeId.Value).ToHashSet();
	}

	public async Task AddAsync(RecipeFavorite favorite, CancellationToken cancellationToken = default)
	{
		await favorites.AddAsync(favorite, cancellationToken);
	}

	public async Task RemoveAsync(OwnerIdentifier owner, RecipeIdentifier recipe, CancellationToken cancellationToken = default)
	{
		await favorites
			.Where(favorite => favorite.Owner == owner && favorite.Recipe == recipe)
			.ExecuteDeleteAsync(cancellationToken);
	}
}
