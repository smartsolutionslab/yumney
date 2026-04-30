using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Services;

public sealed class RecipeIngredientProvider(IRecipeRepository recipes) : IRecipeIngredientProvider
{
	public async Task<IReadOnlyList<RecipeIngredientInfo>> GetIngredientsAsync(
		Guid recipeIdentifier,
		CancellationToken cancellationToken = default)
	{
		var recipe = await recipes.GetByIdAsync(RecipeIdentifier.From(recipeIdentifier), cancellationToken);

		return recipe.Ingredients
			.Select(ingredient => new RecipeIngredientInfo(
				ingredient.Name.Value,
				ingredient.Quantity?.Amount.Value,
				ingredient.Quantity?.Unit?.Value,
				recipe.Servings?.Value))
			.ToList();
	}
}
