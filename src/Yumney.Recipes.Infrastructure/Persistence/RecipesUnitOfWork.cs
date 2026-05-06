using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Domain.RecipeFavorite;
using SmartSolutionsLab.Yumney.Shared.Persistence;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence;

public sealed class RecipesUnitOfWork(RecipesDbContext context)
	: UnitOfWork<RecipesDbContext>(context), IRecipesUnitOfWork
{
	private IRecipeRepository? recipeRepository;
	private IRecipeFavoriteRepository? favoriteRepository;

	public IRecipeRepository Recipes => recipeRepository ??= new RecipeRepository(Context);

	public IRecipeFavoriteRepository Favorites => favoriteRepository ??= new RecipeFavoriteRepository(Context);
}
