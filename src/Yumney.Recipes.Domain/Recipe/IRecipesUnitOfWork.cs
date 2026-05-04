using SmartSolutionsLab.Yumney.Recipes.Domain.RecipeFavorite;
using SmartSolutionsLab.Yumney.Shared.Persistence;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

public interface IRecipesUnitOfWork : IUnitOfWork
{
	IRecipeRepository Recipes { get; }

	IRecipeFavoriteRepository Favorites { get; }
}
