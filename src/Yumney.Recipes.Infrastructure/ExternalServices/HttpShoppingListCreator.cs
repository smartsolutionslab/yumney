using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shopping.Client;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.ExternalServices;

public sealed class HttpShoppingListCreator(IShoppingClient shopping) : IShoppingListCreator
{
	public Task<bool> CreateAsync(CreateShoppingListRequest request, CancellationToken cancellationToken = default)
	{
		var body = new CreateListFromRecipesBody(
			request.Title,
			[.. request.Recipes.Select(recipe => new CreateListRecipeBody(recipe.RecipeIdentifier, recipe.Servings))]);
		return shopping.CreateListFromRecipesAsync(body, cancellationToken);
	}
}
