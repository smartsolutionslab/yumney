namespace SmartSolutionsLab.Yumney.Shared.Common;

/// <summary>
/// Cross-module interface for fetching recipe ingredients.
/// Implemented by Recipes.Infrastructure, consumed by MealPlan.Application.
/// </summary>
public interface IRecipeIngredientProvider
{
    Task<IReadOnlyList<RecipeIngredientInfo>> GetIngredientsAsync(
        Guid recipeIdentifier,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Lightweight ingredient info for cross-module use.
/// </summary>
public sealed record RecipeIngredientInfo(string Name, decimal? Amount, string? Unit, int? RecipeServings);
