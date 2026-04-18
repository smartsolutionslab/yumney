namespace SmartSolutionsLab.Yumney.Recipes.Application.DTOs;

public sealed record RecipeDetailDto(
	Guid Identifier,
	string Title,
	string? Description,
	int? Servings,
	int? PrepTimeMinutes,
	int? CookTimeMinutes,
	string? Difficulty,
	string? ImageUrl,
	string? Language,
	string? SourceUrl,
	DateTime CreatedAt,
	IReadOnlyList<RecipeIngredientDto> Ingredients,
	IReadOnlyList<RecipeStepDto> Steps,
	IReadOnlyList<string> Tags,
	bool IsFavorite);
