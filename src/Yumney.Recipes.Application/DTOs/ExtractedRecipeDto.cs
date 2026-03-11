namespace SmartSolutionsLab.Yumney.Recipes.Application.DTOs;

public sealed record ExtractedRecipeDto(
    string Title,
    IReadOnlyList<ExtractedIngredientDto> Ingredients,
    IReadOnlyList<ExtractedStepDto> Steps,
    string? Description = null,
    int? Servings = null,
    int? PrepTimeMinutes = null,
    int? CookTimeMinutes = null,
    string? Difficulty = null,
    string? ImageUrl = null);
