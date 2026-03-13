namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands;

public sealed record UpdateRecipeRequest(
    string Title,
    string? Description,
    List<SaveRecipeIngredientRequest> Ingredients,
    List<SaveRecipeStepRequest> Steps,
    int? Servings,
    int? PrepTimeMinutes,
    int? CookTimeMinutes,
    string? Difficulty,
    string? ImageUrl);
