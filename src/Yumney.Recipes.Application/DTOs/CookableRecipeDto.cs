namespace SmartSolutionsLab.Yumney.Recipes.Application.DTOs;

/// <summary>
/// One result row from "What Can I Cook?". Pairs a recipe summary with the
/// match classification + the ingredients that are missing (empty for Full).
/// </summary>
public sealed record CookableRecipeDto(
	Guid RecipeIdentifier,
	string Title,
	string? ImageUrl,
	int? Servings,
	int? PrepTimeMinutes,
	int? CookTimeMinutes,
	string? Difficulty,
	int IngredientCount,
	CookableRecipeMatchTier Tier,
	IReadOnlyList<string> MissingIngredients);
