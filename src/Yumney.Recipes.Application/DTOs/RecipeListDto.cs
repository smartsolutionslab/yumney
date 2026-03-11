namespace SmartSolutionsLab.Yumney.Recipes.Application.DTOs;

public sealed record RecipeListDto(
    IReadOnlyList<RecipeListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record RecipeListItemDto(
    Guid Identifier,
    string Title,
    string? Description,
    int? Servings,
    int? PrepTimeMinutes,
    int? CookTimeMinutes,
    string? Difficulty,
    string? ImageUrl,
    DateTime CreatedAt);
