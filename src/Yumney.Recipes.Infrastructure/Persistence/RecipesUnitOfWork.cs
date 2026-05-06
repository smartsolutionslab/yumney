using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Domain.RecipeFavorite;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence;

public sealed class RecipesUnitOfWork(RecipesDbContext context) : IRecipesUnitOfWork
{
	public IRecipeRepository Recipes => field ??= new RecipeRepository(context);

	public IRecipeFavoriteRepository Favorites => field ??= new RecipeFavoriteRepository(context);

	public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
		=> context.SaveChangesAsync(cancellationToken);
}
