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
            .Select(i => new RecipeIngredientInfo(
                i.Name.Value,
                i.Quantity?.Amount.Value,
                i.Quantity?.Unit?.Value,
                recipe.Servings?.Value))
            .ToList();
    }
}
