using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.TestBuilders.Recipes;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Tests;

internal static class RecipeTestData
{
	public static Recipe CreateRecipe(string ownerId = "user-123", string title = "Test Recipe") =>
		RecipeBuilder.A().OwnedBy(ownerId).WithTitle(title).Build();

	public static Recipe CreateRecipeWithSourceUrl(string ownerId = "user-123") =>
		RecipeBuilder.A().OwnedBy(ownerId).WithSourceUrl("https://example.com/recipe").Build();

	public static Recipe CreateRecipeWithOptionals(string ownerId = "user-123") =>
		RecipeBuilder.A()
			.OwnedBy(ownerId)
			.WithTitle("Full Recipe")
			.WithDescription("A test recipe")
			.WithServings(4)
			.WithTiming(prepMinutes: 10, cookMinutes: 20)
			.WithDifficulty("easy")
			.WithImageUrl("https://example.com/image.jpg")
			.WithSourceUrl("https://example.com/recipe")
			.Build();

	public static Recipe CreateRecipeWithIngredients(string ownerId = "user-123") =>
		RecipeBuilder.A()
			.OwnedBy(ownerId)
			.WithTitle("Recipe With Ingredients")
			.WithIngredients([
				IngredientBuilder.A().Named("Flour").WithQuantity(500m, Unit.Gram),
				IngredientBuilder.A().Named("Eggs"),
			])
			.Build();

	public static Recipe CreateRecipeWithSteps(string ownerId = "user-123") =>
		RecipeBuilder.A()
			.OwnedBy(ownerId)
			.WithTitle("Recipe With Steps")
			.WithSteps([
				StepBuilder.A().Numbered(1).WithDescription("Mix flour"),
				StepBuilder.A().Numbered(2).WithDescription("Add eggs"),
			])
			.Build();
}
