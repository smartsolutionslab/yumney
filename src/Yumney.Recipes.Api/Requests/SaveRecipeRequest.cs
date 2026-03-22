namespace SmartSolutionsLab.Yumney.Recipes.Api.Requests;

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
    string? Language = null,
    string? SourceUrl = null,
    List<string>? Tags = null);
