using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe.Events;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Recipe;

public class RecipeTests
{
    [Fact]
    public void Create_ValidInput_CreatesRecipeWithId()
    {
        var recipe = CreateValidRecipe();

        recipe.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_ValidInput_SetsTitle()
    {
        var title = new RecipeTitle("Pasta Carbonara");

        var recipe = CreateValidRecipe(title: title);

        recipe.Title.Should().Be(title);
    }

    [Fact]
    public void Create_WithSourceUrl_SetsSourceUrl()
    {
        var sourceUrl = new RecipeUrl("https://example.com/recipe");

        var recipe = CreateValidRecipe(sourceUrl: sourceUrl);

        recipe.SourceUrl.Should().Be(sourceUrl);
    }

    [Fact]
    public void Create_ValidInput_SetsOwner()
    {
        var owner = new OwnerIdentifier("user-123");

        var recipe = CreateValidRecipe(owner: owner);

        recipe.Owner.Should().Be(owner);
    }

    [Fact]
    public void Create_ValidInput_SetsCreatedAt()
    {
        var before = DateTime.UtcNow;

        var recipe = CreateValidRecipe();

        recipe.CreatedAt.Should().BeOnOrAfter(before);
        recipe.CreatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public void Create_WithoutSourceUrl_SourceUrlIsNull()
    {
        var recipe = CreateValidRecipe();

        recipe.SourceUrl.Should().BeNull();
    }

    [Fact]
    public void Create_ValidInput_SetsIngredients()
    {
        var ingredients = new List<Ingredient>
        {
            Ingredient.Create(new IngredientName("Spaghetti"), new Amount(400), new Unit("g")),
            Ingredient.Create(new IngredientName("Pancetta"), new Amount(200), new Unit("g")),
        };

        var recipe = CreateValidRecipe(ingredients: ingredients);

        recipe.Ingredients.Should().HaveCount(2);
    }

    [Fact]
    public void Create_ValidInput_SetsSteps()
    {
        var steps = new List<Step>
        {
            Step.Create(new StepNumber(1), new StepDescription("Cook pasta")),
            Step.Create(new StepNumber(2), new StepDescription("Fry pancetta")),
        };

        var recipe = CreateValidRecipe(steps: steps);

        recipe.Steps.Should().HaveCount(2);
    }

    [Fact]
    public void Create_WithOptionalFields_SetsAllFields()
    {
        var description = new RecipeDescription("A classic dish");
        var servings = new Servings(4);
        var preparationTime = new PreparationTime(10);
        var cookingTime = new CookingTime(20);
        var difficulty = new Difficulty("medium");
        var imageUrl = new ImageUrl("https://example.com/image.jpg");

        var recipe = CreateValidRecipe(
            description: description,
            servings: servings,
            preparationTime: preparationTime,
            cookingTime: cookingTime,
            difficulty: difficulty,
            imageUrl: imageUrl);

        recipe.Description.Should().Be(description);
        recipe.Servings.Should().Be(servings);
        recipe.PreparationTime.Should().Be(preparationTime);
        recipe.CookingTime.Should().Be(cookingTime);
        recipe.Difficulty.Should().Be(difficulty);
        recipe.ImageUrl.Should().Be(imageUrl);
    }

    [Fact]
    public void Create_WithoutOptionalFields_LeavesNullable()
    {
        var recipe = CreateValidRecipe();

        recipe.Description.Should().BeNull();
        recipe.Servings.Should().BeNull();
        recipe.PreparationTime.Should().BeNull();
        recipe.CookingTime.Should().BeNull();
        recipe.Difficulty.Should().BeNull();
        recipe.ImageUrl.Should().BeNull();
    }

    [Fact]
    public void Create_RaisesRecipeSavedEvent()
    {
        var title = new RecipeTitle("Pasta Carbonara");

        var recipe = CreateValidRecipe(title: title);

        recipe.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<RecipeSavedEvent>()
            .Which.Title.Should().Be(title);
    }

    [Fact]
    public void Create_GeneratesUniqueIds()
    {
        var recipe1 = CreateValidRecipe();
        var recipe2 = CreateValidRecipe();

        recipe1.Id.Should().NotBe(recipe2.Id);
    }

    [Fact]
    public void Create_EmptyIngredients_ThrowsGuardException()
    {
        var act = () => CreateValidRecipe(ingredients: []);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Create_EmptySteps_ThrowsGuardException()
    {
        var act = () => CreateValidRecipe(steps: []);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Create_RecipeSavedEvent_ContainsRecipeId()
    {
        var recipe = CreateValidRecipe();

        var domainEvent = recipe.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<RecipeSavedEvent>().Subject;

        domainEvent.RecipeIdentifier.Should().Be(recipe.Id);
    }

    private static Domain.Recipe.Recipe CreateValidRecipe(
        RecipeTitle? title = null,
        RecipeUrl? sourceUrl = null,
        OwnerIdentifier? owner = null,
        IReadOnlyList<Ingredient>? ingredients = null,
        IReadOnlyList<Step>? steps = null,
        RecipeDescription? description = null,
        Servings? servings = null,
        PreparationTime? preparationTime = null,
        CookingTime? cookingTime = null,
        Difficulty? difficulty = null,
        ImageUrl? imageUrl = null)
    {
        return Domain.Recipe.Recipe.Create(
            title ?? new RecipeTitle("Test Recipe"),
            owner ?? new OwnerIdentifier("user-123"),
            ingredients ?? [Ingredient.Create(new IngredientName("Flour"), new Amount(500), new Unit("g"))],
            steps ?? [Step.Create(new StepNumber(1), new StepDescription("Mix ingredients"))],
            description,
            servings,
            preparationTime,
            cookingTime,
            difficulty,
            imageUrl,
            sourceUrl);
    }
}
