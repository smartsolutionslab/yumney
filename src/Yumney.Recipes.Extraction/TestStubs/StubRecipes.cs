using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.TestStubs;

internal static class StubRecipes
{
	public static ExtractedRecipeDto Sample(string? titleSuffix = null) =>
		new(
			Title: titleSuffix is null ? "Stub Recipe" : $"Stub Recipe {titleSuffix}",
			Ingredients:
			[
				new ExtractedIngredientDto("Flour", 200m, "g"),
				new ExtractedIngredientDto("Water", 100m, "ml"),
			],
			Steps:
			[
				new ExtractedStepDto(1, "Mix flour and water."),
				new ExtractedStepDto(2, "Bake for 20 minutes."),
			],
			Description: "A stub recipe used by E2E test mode.",
			Language: "en",
			Servings: 2,
			PrepTimeMinutes: 5,
			CookTimeMinutes: 20,
			Difficulty: "easy",
			ImageUrl: null);
}
