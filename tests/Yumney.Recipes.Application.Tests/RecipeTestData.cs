using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Tests;

internal static class RecipeTestData
{
    public static Recipe CreateRecipe(string ownerId = "user-123", string title = "Test Recipe")
    {
        return Recipe.Create(
            RecipeTitle.From(title),
            OwnerIdentifier.From(ownerId),
            [Ingredient.Create(IngredientName.From("Flour"), null)],
            [Step.Create(StepNumber.From(1), StepDescription.From("Mix"))]);
    }

    public static Recipe CreateRecipeWithSourceUrl(string ownerId = "user-123")
    {
        return Recipe.Create(
            RecipeTitle.From("Test Recipe"),
            OwnerIdentifier.From(ownerId),
            [Ingredient.Create(IngredientName.From("Flour"), null)],
            [Step.Create(StepNumber.From(1), StepDescription.From("Mix"))],
            sourceUrl: RecipeUrl.From("https://example.com/recipe"));
    }

    public static Recipe CreateRecipeWithOptionals(string ownerId = "user-123")
    {
        return Recipe.Create(
            RecipeTitle.From("Full Recipe"),
            OwnerIdentifier.From(ownerId),
            [Ingredient.Create(IngredientName.From("Flour"), null)],
            [Step.Create(StepNumber.From(1), StepDescription.From("Mix"))],
            RecipeDescription.From("A test recipe"),
            Servings.From(4),
            TimingInfo.Of(PreparationTime.From(10), CookingTime.From(20)),
            Difficulty.From("easy"),
            ImageUrl.From("https://example.com/image.jpg"),
            sourceUrl: RecipeUrl.From("https://example.com/recipe"));
    }

    public static Recipe CreateRecipeWithIngredients(string ownerId = "user-123")
    {
        return Recipe.Create(
            RecipeTitle.From("Recipe With Ingredients"),
            OwnerIdentifier.From(ownerId),
            [
                Ingredient.Create(IngredientName.From("Flour"), Quantity.Of(Amount.From(500m), Unit.Gram)),
                Ingredient.Create(IngredientName.From("Eggs"), null),
            ],
            [Step.Create(StepNumber.From(1), StepDescription.From("Mix"))]);
    }

    public static Recipe CreateRecipeWithSteps(string ownerId = "user-123")
    {
        return Recipe.Create(
            RecipeTitle.From("Recipe With Steps"),
            OwnerIdentifier.From(ownerId),
            [Ingredient.Create(IngredientName.From("Flour"), null)],
            [
                Step.Create(StepNumber.From(1), StepDescription.From("Mix flour")),
                Step.Create(StepNumber.From(2), StepDescription.From("Add eggs")),
            ]);
    }
}
