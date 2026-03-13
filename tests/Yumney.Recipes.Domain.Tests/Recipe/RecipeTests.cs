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

        domainEvent.RecipeIdentifier.Value.Should().Be(recipe.Id);
    }

    [Fact]
    public void Update_ValidInput_SetsAllEditableFields()
    {
        var recipe = CreateValidRecipe(
            description: new RecipeDescription("Old description"),
            servings: new Servings(2));

        var newTitle = new RecipeTitle("Updated Recipe");
        var newDescription = new RecipeDescription("Updated description");
        var newServings = new Servings(6);
        var newPrepTime = new PreparationTime(15);
        var newCookTime = new CookingTime(30);
        var newDifficulty = new Difficulty("hard");
        var newImageUrl = new ImageUrl("https://example.com/new.jpg");
        var newIngredients = new List<Ingredient>
        {
            Ingredient.Create(new IngredientName("Butter"), new Amount(100), new Unit("g")),
        };
        var newSteps = new List<Step>
        {
            Step.Create(new StepNumber(1), new StepDescription("Melt butter")),
        };

        recipe.Update(newTitle, newIngredients, newSteps, newDescription, newServings, newPrepTime, newCookTime, newDifficulty, newImageUrl);

        recipe.Title.Should().Be(newTitle);
        recipe.Description.Should().Be(newDescription);
        recipe.Servings.Should().Be(newServings);
        recipe.PreparationTime.Should().Be(newPrepTime);
        recipe.CookingTime.Should().Be(newCookTime);
        recipe.Difficulty.Should().Be(newDifficulty);
        recipe.ImageUrl.Should().Be(newImageUrl);
    }

    [Fact]
    public void Update_ReplacesIngredients()
    {
        var recipe = CreateValidRecipe(ingredients:
        [
            Ingredient.Create(new IngredientName("Flour"), new Amount(500), new Unit("g")),
            Ingredient.Create(new IngredientName("Sugar"), new Amount(200), new Unit("g")),
        ]);

        var newIngredients = new List<Ingredient>
        {
            Ingredient.Create(new IngredientName("Butter"), new Amount(100), new Unit("g")),
        };

        recipe.Update(new RecipeTitle("Updated"), newIngredients, [Step.Create(new StepNumber(1), new StepDescription("Mix"))]);

        recipe.Ingredients.Should().HaveCount(1);
        recipe.Ingredients[0].Name.Value.Should().Be("Butter");
    }

    [Fact]
    public void Update_ReplacesSteps()
    {
        var recipe = CreateValidRecipe(steps:
        [
            Step.Create(new StepNumber(1), new StepDescription("Step one")),
            Step.Create(new StepNumber(2), new StepDescription("Step two")),
        ]);

        var newSteps = new List<Step>
        {
            Step.Create(new StepNumber(1), new StepDescription("New only step")),
        };

        recipe.Update(new RecipeTitle("Updated"), [Ingredient.Create(new IngredientName("Flour"), null, null)], newSteps);

        recipe.Steps.Should().HaveCount(1);
        recipe.Steps[0].Description.Value.Should().Be("New only step");
    }

    [Fact]
    public void Update_RaisesRecipeUpdatedEvent()
    {
        var recipe = CreateValidRecipe();
        recipe.ClearDomainEvents();

        var newTitle = new RecipeTitle("Updated Recipe");

        recipe.Update(
            newTitle,
            [Ingredient.Create(new IngredientName("Flour"), null, null)],
            [Step.Create(new StepNumber(1), new StepDescription("Mix"))]);

        recipe.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<RecipeUpdatedEvent>()
            .Which.Title.Should().Be(newTitle);
    }

    [Fact]
    public void Update_RecipeUpdatedEvent_ContainsRecipeId()
    {
        var recipe = CreateValidRecipe();
        recipe.ClearDomainEvents();

        recipe.Update(
            new RecipeTitle("Updated"),
            [Ingredient.Create(new IngredientName("Flour"), null, null)],
            [Step.Create(new StepNumber(1), new StepDescription("Mix"))]);

        var domainEvent = recipe.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<RecipeUpdatedEvent>().Subject;

        domainEvent.RecipeIdentifier.Value.Should().Be(recipe.Id);
    }

    [Fact]
    public void Update_EmptyIngredients_ThrowsGuardException()
    {
        var recipe = CreateValidRecipe();

        var act = () => recipe.Update(
            new RecipeTitle("Updated"),
            [],
            [Step.Create(new StepNumber(1), new StepDescription("Mix"))]);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Update_EmptySteps_ThrowsGuardException()
    {
        var recipe = CreateValidRecipe();

        var act = () => recipe.Update(
            new RecipeTitle("Updated"),
            [Ingredient.Create(new IngredientName("Flour"), null, null)],
            []);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Update_DoesNotChangeOwner()
    {
        var owner = new OwnerIdentifier("user-123");
        var recipe = CreateValidRecipe(owner: owner);

        recipe.Update(
            new RecipeTitle("Updated"),
            [Ingredient.Create(new IngredientName("Flour"), null, null)],
            [Step.Create(new StepNumber(1), new StepDescription("Mix"))]);

        recipe.Owner.Should().Be(owner);
    }

    [Fact]
    public void Update_DoesNotChangeSourceUrl()
    {
        var sourceUrl = new RecipeUrl("https://example.com/recipe");
        var recipe = CreateValidRecipe(sourceUrl: sourceUrl);

        recipe.Update(
            new RecipeTitle("Updated"),
            [Ingredient.Create(new IngredientName("Flour"), null, null)],
            [Step.Create(new StepNumber(1), new StepDescription("Mix"))]);

        recipe.SourceUrl.Should().Be(sourceUrl);
    }

    [Fact]
    public void Update_DoesNotChangeCreatedAt()
    {
        var recipe = CreateValidRecipe();
        var originalCreatedAt = recipe.CreatedAt;

        recipe.Update(
            new RecipeTitle("Updated"),
            [Ingredient.Create(new IngredientName("Flour"), null, null)],
            [Step.Create(new StepNumber(1), new StepDescription("Mix"))]);

        recipe.CreatedAt.Should().Be(originalCreatedAt);
    }

    [Fact]
    public void Update_WithoutOptionalFields_ClearsExistingValues()
    {
        var recipe = CreateValidRecipe(
            description: new RecipeDescription("Old"),
            servings: new Servings(4),
            preparationTime: new PreparationTime(10),
            cookingTime: new CookingTime(20),
            difficulty: new Difficulty("easy"),
            imageUrl: new ImageUrl("https://example.com/old.jpg"));

        recipe.Update(
            new RecipeTitle("Updated"),
            [Ingredient.Create(new IngredientName("Flour"), null, null)],
            [Step.Create(new StepNumber(1), new StepDescription("Mix"))]);

        recipe.Description.Should().BeNull();
        recipe.Servings.Should().BeNull();
        recipe.PreparationTime.Should().BeNull();
        recipe.CookingTime.Should().BeNull();
        recipe.Difficulty.Should().BeNull();
        recipe.ImageUrl.Should().BeNull();
    }

    [Fact]
    public void MarkAsDeleted_RaisesRecipeDeletedEvent()
    {
        var recipe = CreateValidRecipe();
        recipe.ClearDomainEvents();

        recipe.MarkAsDeleted();

        recipe.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<RecipeDeletedEvent>();
    }

    [Fact]
    public void MarkAsDeleted_EventContainsRecipeIdentifier()
    {
        var recipe = CreateValidRecipe();
        recipe.ClearDomainEvents();

        recipe.MarkAsDeleted();

        var domainEvent = recipe.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<RecipeDeletedEvent>().Subject;

        domainEvent.RecipeIdentifier.Value.Should().Be(recipe.Id);
    }

    [Fact]
    public void MarkAsDeleted_EventContainsTitle()
    {
        var title = new RecipeTitle("Pasta Carbonara");
        var recipe = CreateValidRecipe(title: title);
        recipe.ClearDomainEvents();

        recipe.MarkAsDeleted();

        var domainEvent = recipe.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<RecipeDeletedEvent>().Subject;

        domainEvent.Title.Should().Be(title);
    }

    [Fact]
    public void MarkAsDeleted_EventContainsOwner()
    {
        var owner = new OwnerIdentifier("user-123");
        var recipe = CreateValidRecipe(owner: owner);
        recipe.ClearDomainEvents();

        recipe.MarkAsDeleted();

        var domainEvent = recipe.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<RecipeDeletedEvent>().Subject;

        domainEvent.Owner.Should().Be(owner);
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
