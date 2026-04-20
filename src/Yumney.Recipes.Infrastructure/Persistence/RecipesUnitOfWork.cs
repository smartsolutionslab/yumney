using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Domain.RecipeFavorite;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence;

public sealed class RecipesUnitOfWork(
	RecipesDbContext context,
	IRecipeRepository recipes,
	IRecipeFavoriteRepository favorites) : IRecipesUnitOfWork
{
	public IRecipeRepository Recipes => recipes;

	public IRecipeFavoriteRepository Favorites => favorites;

	public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
		=> context.SaveChangesAsync(cancellationToken);
}
