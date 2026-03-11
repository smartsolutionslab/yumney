namespace Yumney.Recipes.Application.DTOs;

public sealed record RecipeDetailDto(
    Guid Identifier,
    string Title,
    string? Description,
    int? Servings,
    int? PrepTimeMinutes,
    int? CookTimeMinutes,
    string? Difficulty,
    string? ImageUrl,
    string? SourceUrl,
    DateTime CreatedAt,
    IReadOnlyList<RecipeIngredientDto> Ingredients,
    IReadOnlyList<RecipeStepDto> Steps);

public sealed record RecipeIngredientDto(string Name, decimal? Amount, string? Unit);

public sealed record RecipeStepDto(int Number, string Description);
