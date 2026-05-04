using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Application.DTOs;

public static class RecipeMappingExtensions
{
	public static RecipeDetailDto ToDetailDto(this Recipe recipe, bool isFavorite = false) =>
		new(
			recipe.Id.Value,
			recipe.Title.Value,
			recipe.Description?.Value,
			recipe.Servings?.Value,
			recipe.Timing?.Preparation?.Value,
			recipe.Timing?.Cooking?.Value,
			recipe.Difficulty?.Value,
			recipe.ImageUrl?.Value,
			recipe.Language?.Value,
			recipe.SourceUrl?.Value,
			recipe.CreatedAt,
			recipe.Ingredients.ToDtos(),
			recipe.Steps.ToDtos(),
			recipe.Tags.Select(tag => tag.Value).ToList(),
			isFavorite,
			recipe.Rating?.Value,
			recipe.Notes?.Value);

	public static RecipeListItemDto ToListItemDto(this Recipe recipe, bool isFavorite = false) =>
		new(
			recipe.Id.Value,
			recipe.Title.Value,
			recipe.Description?.Value,
			recipe.Servings?.Value,
			recipe.Timing?.Preparation?.Value,
			recipe.Timing?.Cooking?.Value,
			recipe.Difficulty?.Value,
			recipe.ImageUrl?.Value,
			recipe.CreatedAt,
			recipe.Tags.Select(tag => tag.Value).ToList(),
			isFavorite,
			recipe.Rating?.Value,
			recipe.Notes is not null);

	public static SavedRecipeDto ToSavedDto(this Recipe recipe) =>
		new(recipe.Id.Value, recipe.Title.Value, recipe.CreatedAt);

	public static CookableRecipeDto ToCookableDto(
		this Recipe recipe,
		CookableRecipeMatchTier tier,
		IReadOnlyList<string> missingIngredients) =>
		new(
			RecipeIdentifier: recipe.Id.Value,
			Title: recipe.Title.Value,
			ImageUrl: recipe.ImageUrl?.Value,
			Servings: recipe.Servings?.Value,
			PrepTimeMinutes: recipe.Timing?.Preparation?.Value,
			CookTimeMinutes: recipe.Timing?.Cooking?.Value,
			Difficulty: recipe.Difficulty?.Value,
			IngredientCount: recipe.Ingredients.Count,
			Tier: tier,
			MissingIngredients: missingIngredients);

	public static RecipeIngredientDto ToDto(this Ingredient ingredient) =>
		new(
			ingredient.Name.Value,
			ingredient.Quantity?.Amount.Value,
			ingredient.Quantity?.Unit?.Value);

	public static IReadOnlyList<RecipeIngredientDto> ToDtos(this IEnumerable<Ingredient> ingredients) =>
		ingredients.Select(ingredient => ingredient.ToDto()).ToList();

	public static RecipeStepDto ToDto(this Step step) =>
		new(step.Number.Value, step.Description.Value);

	public static IReadOnlyList<RecipeStepDto> ToDtos(this IEnumerable<Step> steps) =>
		steps.Select(step => step.ToDto()).ToList();
}
