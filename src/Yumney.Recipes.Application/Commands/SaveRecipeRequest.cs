namespace Yumney.Recipes.Application.Commands;

public sealed record SaveRecipeRequest(
    string Title,
    string? Description,
    List<SaveRecipeIngredientRequest> Ingredients,
    List<SaveRecipeStepRequest> Steps,
    int? Servings,
    int? PrepTimeMinutes,
    int? CookTimeMinutes,
    string? Difficulty,
    string? ImageUrl,
    string? SourceUrl = null);

public sealed record SaveRecipeIngredientRequest(string Name, decimal? Amount, string? Unit);

public sealed record SaveRecipeStepRequest(int Number, string Description);
