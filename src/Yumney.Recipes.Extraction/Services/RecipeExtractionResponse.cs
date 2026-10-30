using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.Services;

/// <summary>
/// Wire shape of an extraction reply. Flat and fully optional so one schema
/// covers both a recipe and the <c>{ "error": "NO_RECIPE_FOUND" }</c> sentinel —
/// the same shape the streaming import endpoint parses field by field.
/// </summary>
internal sealed record RecipeExtractionResponse(
	string? Error,
	string? Title,
	string? Description,
	string? Language,
	IReadOnlyList<ExtractedIngredientDto>? Ingredients,
	IReadOnlyList<ExtractedStepDto>? Steps,
	int? Servings,
	int? PrepTimeMinutes,
	int? CookTimeMinutes,
	string? Difficulty,
	string? ImageUrl)
{
	public bool IsNoRecipeFound => Error?.Contains(ExtractionPrompts.LlmNoRecipeErrorCode, StringComparison.Ordinal) == true;

	public ExtractedRecipeDto? ToDto() => string.IsNullOrWhiteSpace(Title)
		? null
		: new ExtractedRecipeDto(
			Title,
			Ingredients ?? [],
			Steps ?? [],
			Description,
			Language,
			Servings,
			PrepTimeMinutes,
			CookTimeMinutes,
			Difficulty,
			ImageUrl);
}
