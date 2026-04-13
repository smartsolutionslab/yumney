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

        recipe.Id.Should().NotBeNull();
    }

    [Fact]
    public void Create_ValidInput_SetsTitle()
    {
        var title = RecipeTitle.From("Pasta Carbonara");

        var recipe = CreateValidRecipe(title: title);

        recipe.Title.Should().Be(title);
    }

    [Fact]
    public void Create_WithSourceUrl_SetsSourceUrl()
    {
        var sourceUrl = RecipeUrl.From("https://example.com/recipe");

        var recipe = CreateValidRecipe(sourceUrl: sourceUrl);

        recipe.SourceUrl.Should().Be(sourceUrl);
    }

    [Fact]
    public void Create_ValidInput_SetsOwner()
    {
        var owner = OwnerIdentifier.From("user-123");

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
        List<Ingredient> ingredients =
        [
            Ingredient.Create(IngredientName.From("Spaghetti"), Quantity.Of(Amount.From(400), Unit.From("g"))),
            Ingredient.Create(IngredientName.From("Pancetta"), Quantity.Of(Amount.From(200), Unit.From("g"))),
        ];

        var recipe = CreateValidRecipe(ingredients: ingredients);

        recipe.Ingredients.Should().HaveCount(2);
    }

    [Fact]
    public void Create_ValidInput_SetsSteps()
    {
        List<Step> steps =
        [
            Step.Create(StepNumber.From(1), StepDescription.From("Cook pasta")),
            Step.Create(StepNumber.From(2), StepDescription.From("Fry pancetta")),
        ];

        var recipe = CreateValidRecipe(steps: steps);

        recipe.Steps.Should().HaveCount(2);
    }

    [Fact]
    public void Create_WithOptionalFields_SetsAllFields()
    {
        var description = RecipeDescription.From("A classic dish");
        var servings = Servings.From(4);
        var preparationTime = PreparationTime.From(10);
        var cookingTime = CookingTime.From(20);
        var difficulty = Difficulty.From("medium");
        var imageUrl = ImageUrl.From("https://example.com/image.jpg");

        var recipe = CreateValidRecipe(
            description: description,
            servings: servings,
            preparationTime: preparationTime,
            cookingTime: cookingTime,
            difficulty: difficulty,
            imageUrl: imageUrl);

        recipe.Description.Should().Be(description);
        recipe.Servings.Should().Be(servings);
        recipe.Timing?.Preparation.Should().Be(preparationTime);
        recipe.Timing?.Cooking.Should().Be(cookingTime);
        recipe.Difficulty.Should().Be(difficulty);
        recipe.ImageUrl.Should().Be(imageUrl);
    }

    [Fact]
    public void Create_WithoutOptionalFields_LeavesNullable()
    {
        var recipe = CreateValidRecipe();

        recipe.Description.Should().BeNull();
        recipe.Servings.Should().BeNull();
        recipe.Timing?.Preparation.Should().BeNull();
        recipe.Timing?.Cooking.Should().BeNull();
        recipe.Difficulty.Should().BeNull();
        recipe.ImageUrl.Should().BeNull();
    }

    [Fact]
    public void Create_RaisesRecipeSavedEvent()
    {
        var title = RecipeTitle.From("Pasta Carbonara");

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

    [Fact]
    public void Update_ValidInput_SetsTitle()
    {
        var recipe = CreateRecipeWithUpdatedFields();

        recipe.Title.Should().Be(RecipeTitle.From("Updated Recipe"));
    }

    [Fact]
    public void Update_ValidInput_SetsDescription()
    {
        var recipe = CreateRecipeWithUpdatedFields();

        recipe.Description.Should().Be(RecipeDescription.From("Updated description"));
    }

    [Fact]
    public void Update_ValidInput_SetsServings()
    {
        var recipe = CreateRecipeWithUpdatedFields();

        recipe.Servings.Should().Be(Servings.From(6));
    }

    [Fact]
    public void Update_ValidInput_SetsPreparationTime()
    {
        var recipe = CreateRecipeWithUpdatedFields();

        recipe.Timing?.Preparation.Should().Be(PreparationTime.From(15));
    }

    [Fact]
    public void Update_ValidInput_SetsCookingTime()
    {
        var recipe = CreateRecipeWithUpdatedFields();

        recipe.Timing?.Cooking.Should().Be(CookingTime.From(30));
    }

    [Fact]
    public void Update_ValidInput_SetsDifficulty()
    {
        var recipe = CreateRecipeWithUpdatedFields();

        recipe.Difficulty.Should().Be(Difficulty.From("hard"));
    }

    [Fact]
    public void Update_ValidInput_SetsImageUrl()
    {
        var recipe = CreateRecipeWithUpdatedFields();

        recipe.ImageUrl.Should().Be(ImageUrl.From("https://example.com/new.jpg"));
    }

    [Fact]
    public void Update_ReplacesIngredients()
    {
        var recipe = CreateValidRecipe(ingredients:
        [
            Ingredient.Create(IngredientName.From("Flour"), Quantity.Of(Amount.From(500), Unit.From("g"))),
            Ingredient.Create(IngredientName.From("Sugar"), Quantity.Of(Amount.From(200), Unit.From("g"))),
        ]);

        List<Ingredient> newIngredients =
        [
            Ingredient.Create(IngredientName.From("Butter"), Quantity.Of(Amount.From(100), Unit.From("g"))),
        ];

        recipe.Update(RecipeTitle.From("Updated"), newIngredients, [Step.Create(StepNumber.From(1), StepDescription.From("Mix"))]);

        recipe.Ingredients.Should().HaveCount(1);
        recipe.Ingredients[0].Name.Value.Should().Be("Butter");
    }

    [Fact]
    public void Update_ReplacesSteps()
    {
        var recipe = CreateValidRecipe(steps:
        [
            Step.Create(StepNumber.From(1), StepDescription.From("Step one")),
            Step.Create(StepNumber.From(2), StepDescription.From("Step two")),
        ]);

        List<Step> newSteps =
        [
            Step.Create(StepNumber.From(1), StepDescription.From("New only step")),
        ];

        recipe.Update(RecipeTitle.From("Updated"), [Ingredient.Create(IngredientName.From("Flour"), null)], newSteps);

        recipe.Steps.Should().HaveCount(1);
        recipe.Steps[0].Description.Value.Should().Be("New only step");
    }

    [Fact]
    public void Update_RaisesRecipeUpdatedEvent()
    {
        var recipe = CreateValidRecipe();
        recipe.ClearDomainEvents();

        var newTitle = RecipeTitle.From("Updated Recipe");

        recipe.Update(
            newTitle,
            [Ingredient.Create(IngredientName.From("Flour"), null)],
            [Step.Create(StepNumber.From(1), StepDescription.From("Mix"))]);

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
            RecipeTitle.From("Updated"),
            [Ingredient.Create(IngredientName.From("Flour"), null)],
            [Step.Create(StepNumber.From(1), StepDescription.From("Mix"))]);

        var domainEvent = recipe.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<RecipeUpdatedEvent>().Subject;

        domainEvent.RecipeIdentifier.Should().Be(recipe.Id);
    }

    [Fact]
    public void Update_EmptyIngredients_ThrowsGuardException()
    {
        var recipe = CreateValidRecipe();

        var act = () => recipe.Update(
            RecipeTitle.From("Updated"),
            [],
            [Step.Create(StepNumber.From(1), StepDescription.From("Mix"))]);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Update_EmptySteps_ThrowsGuardException()
    {
        var recipe = CreateValidRecipe();

        var act = () => recipe.Update(
            RecipeTitle.From("Updated"),
            [Ingredient.Create(IngredientName.From("Flour"), null)],
            []);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Update_DoesNotChangeOwner()
    {
        var owner = OwnerIdentifier.From("user-123");
        var recipe = CreateValidRecipe(owner: owner);

        recipe.Update(
            RecipeTitle.From("Updated"),
            [Ingredient.Create(IngredientName.From("Flour"), null)],
            [Step.Create(StepNumber.From(1), StepDescription.From("Mix"))]);

        recipe.Owner.Should().Be(owner);
    }

    [Fact]
    public void Update_DoesNotChangeSourceUrl()
    {
        var sourceUrl = RecipeUrl.From("https://example.com/recipe");
        var recipe = CreateValidRecipe(sourceUrl: sourceUrl);

        recipe.Update(
            RecipeTitle.From("Updated"),
            [Ingredient.Create(IngredientName.From("Flour"), null)],
            [Step.Create(StepNumber.From(1), StepDescription.From("Mix"))]);

        recipe.SourceUrl.Should().Be(sourceUrl);
    }

    [Fact]
    public void Update_DoesNotChangeCreatedAt()
    {
        var recipe = CreateValidRecipe();
        var originalCreatedAt = recipe.CreatedAt;

        recipe.Update(
            RecipeTitle.From("Updated"),
            [Ingredient.Create(IngredientName.From("Flour"), null)],
            [Step.Create(StepNumber.From(1), StepDescription.From("Mix"))]);

        recipe.CreatedAt.Should().Be(originalCreatedAt);
    }

    [Fact]
    public void Update_WithoutOptionalFields_ClearsExistingValues()
    {
        var recipe = CreateValidRecipe(
            description: RecipeDescription.From("Old"),
            servings: Servings.From(4),
            preparationTime: PreparationTime.From(10),
            cookingTime: CookingTime.From(20),
            difficulty: Difficulty.From("easy"),
            imageUrl: ImageUrl.From("https://example.com/old.jpg"));

        recipe.Update(
            RecipeTitle.From("Updated"),
            [Ingredient.Create(IngredientName.From("Flour"), null)],
            [Step.Create(StepNumber.From(1), StepDescription.From("Mix"))]);

        recipe.Description.Should().BeNull();
        recipe.Servings.Should().BeNull();
        recipe.Timing?.Preparation.Should().BeNull();
        recipe.Timing?.Cooking.Should().BeNull();
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

        domainEvent.RecipeIdentifier.Should().Be(recipe.Id);
    }

    [Fact]
    public void MarkAsDeleted_EventContainsTitle()
    {
        var title = RecipeTitle.From("Pasta Carbonara");
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
        var owner = OwnerIdentifier.From("user-123");
        var recipe = CreateValidRecipe(owner: owner);
        recipe.ClearDomainEvents();

        recipe.MarkAsDeleted();

        var domainEvent = recipe.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<RecipeDeletedEvent>().Subject;

        domainEvent.Owner.Should().Be(owner);
    }

    [Fact]
    public void Create_WithTags_SetsTags()
    {
        List<RecipeTag> tags = [RecipeTag.From("italian"), RecipeTag.From("pasta")];

        var recipe = CreateValidRecipe(tags: tags);

        recipe.Tags.Should().HaveCount(2);
        recipe.Tags[0].Value.Should().Be("italian");
        recipe.Tags[1].Value.Should().Be("pasta");
    }

    [Fact]
    public void Create_WithoutTags_TagsEmpty()
    {
        var recipe = CreateValidRecipe();

        recipe.Tags.Should().BeEmpty();
    }

    [Fact]
    public void Update_WithTags_ReplacesTags()
    {
        var recipe = CreateValidRecipe(tags: [RecipeTag.From("old-tag")]);

        recipe.Update(
            RecipeTitle.From("Updated"),
            [Ingredient.Create(IngredientName.From("Flour"), null)],
            [Step.Create(StepNumber.From(1), StepDescription.From("Mix"))],
            tags: [RecipeTag.From("new-tag")]);

        recipe.Tags.Should().ContainSingle().Which.Value.Should().Be("new-tag");
    }

    [Fact]
    public void Update_WithNullTags_ClearsTags()
    {
        var recipe = CreateValidRecipe(tags: [RecipeTag.From("old-tag")]);

        recipe.Update(
            RecipeTitle.From("Updated"),
            [Ingredient.Create(IngredientName.From("Flour"), null)],
            [Step.Create(StepNumber.From(1), StepDescription.From("Mix"))]);

        recipe.Tags.Should().BeEmpty();
    }

    private static Domain.Recipe.Recipe CreateRecipeWithUpdatedFields()
    {
        var recipe = CreateValidRecipe(
            description: RecipeDescription.From("Old description"),
            servings: Servings.From(2));

        List<Ingredient> newIngredients =
        [
            Ingredient.Create(IngredientName.From("Butter"), Quantity.Of(Amount.From(100), Unit.From("g"))),
        ];
        List<Step> newSteps =
        [
            Step.Create(StepNumber.From(1), StepDescription.From("Melt butter")),
        ];

        recipe.Update(
            RecipeTitle.From("Updated Recipe"),
            newIngredients,
            newSteps,
            RecipeDescription.From("Updated description"),
            Servings.From(6),
            TimingInfo.FromNullable(15, 30),
            Difficulty.From("hard"),
            ImageUrl.From("https://example.com/new.jpg"));

        return recipe;
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
        ImageUrl? imageUrl = null,
        IReadOnlyList<RecipeTag>? tags = null)
    {
        return Domain.Recipe.Recipe.Create(
            title ?? RecipeTitle.From("Test Recipe"),
            owner ?? OwnerIdentifier.From("user-123"),
            ingredients ?? [Ingredient.Create(IngredientName.From("Flour"), Quantity.Of(Amount.From(500), Unit.From("g")))],
            steps ?? [Step.Create(StepNumber.From(1), StepDescription.From("Mix ingredients"))],
            description,
            servings,
            TimingInfo.FromNullable(preparationTime, cookingTime),
            difficulty,
            imageUrl,
            sourceUrl: sourceUrl,
            tags: tags);
    }
}
