using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Tests.Commands;

public class UpdateRecipeCommandHandlerTests
{
    private readonly IRecipeRepository recipes = Substitute.For<IRecipeRepository>();
    private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
    private readonly ILogger<UpdateRecipeCommandHandler> logger = Substitute.For<ILogger<UpdateRecipeCommandHandler>>();
    private readonly UpdateRecipeCommandHandler handler;

    public UpdateRecipeCommandHandlerTests()
    {
        currentUser.UserId.Returns("user-123");
        handler = new UpdateRecipeCommandHandler(recipes, currentUser, logger);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_ReturnsSuccess()
    {
        var recipe = CreateTestRecipe("user-123");
        recipes.GetByIdAsync(recipe.Id, Arg.Any<CancellationToken>()).Returns(recipe);

        var command = CreateValidCommand(recipe.Id);

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_ReturnsRecipeDetailDto()
    {
        var recipe = CreateTestRecipe("user-123");
        recipes.GetByIdAsync(recipe.Id, Arg.Any<CancellationToken>()).Returns(recipe);

        var command = CreateValidCommand(recipe.Id);

        var result = await handler.HandleAsync(command);

        result.Value.Identifier.Should().Be(recipe.Id.Value);
        result.Value.Title.Should().Be("Updated Pasta");
        result.Value.Ingredients.Should().HaveCount(1);
        result.Value.Steps.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleAsync_RecipeNotFound_ReturnsFailure()
    {
        var recipeId = RecipeIdentifier.New();
        recipes.GetByIdAsync(recipeId, Arg.Any<CancellationToken>()).Returns((Recipe?)null);

        var command = CreateValidCommand(recipeId);

        var result = await handler.HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UpdateRecipeErrors.NotFound);
    }

    [Fact]
    public async Task HandleAsync_WrongOwner_ReturnsAccessDenied()
    {
        var recipe = CreateTestRecipe("other-user");
        recipes.GetByIdAsync(recipe.Id, Arg.Any<CancellationToken>()).Returns(recipe);

        var command = CreateValidCommand(recipe.Id);

        var result = await handler.HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UpdateRecipeErrors.AccessDenied);
    }

    [Fact]
    public async Task HandleAsync_RecipeNotFound_DoesNotCallUpdateAsync()
    {
        var recipeId = RecipeIdentifier.New();
        recipes.GetByIdAsync(recipeId, Arg.Any<CancellationToken>()).Returns((Recipe?)null);

        var command = CreateValidCommand(recipeId);

        await handler.HandleAsync(command);

        await recipes.DidNotReceive().UpdateAsync(Arg.Any<Recipe>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WrongOwner_DoesNotCallUpdateAsync()
    {
        var recipe = CreateTestRecipe("other-user");
        recipes.GetByIdAsync(recipe.Id, Arg.Any<CancellationToken>()).Returns(recipe);

        var command = CreateValidCommand(recipe.Id);

        await handler.HandleAsync(command);

        await recipes.DidNotReceive().UpdateAsync(Arg.Any<Recipe>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_CallsUpdateAsync()
    {
        var recipe = CreateTestRecipe("user-123");
        var recipeId = recipe.Id;
        recipes.GetByIdAsync(recipeId, Arg.Any<CancellationToken>()).Returns(recipe);

        var command = CreateValidCommand(recipeId);

        await handler.HandleAsync(command);

        await recipes.Received(1).UpdateAsync(recipe, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_UpdatesRecipeFields()
    {
        var recipe = CreateTestRecipe("user-123");
        var recipeId = recipe.Id;
        recipes.GetByIdAsync(recipeId, Arg.Any<CancellationToken>()).Returns(recipe);

        var command = new UpdateRecipeCommand(
            recipeId,
            RecipeTitle.From("New Title"),
            [new SaveRecipeIngredientItem(IngredientName.From("Butter"), Amount.From(200), Unit.From("g"))],
            [new SaveRecipeStepItem(StepNumber.From(1), StepDescription.From("Melt butter"))],
            RecipeDescription.From("New description"),
            Servings.From(6),
            PreparationTime.From(15),
            CookingTime.From(30),
            Difficulty.From("hard"),
            ImageUrl.From("https://example.com/new.jpg"));

        var result = await handler.HandleAsync(command);

        result.Value.Title.Should().Be("New Title");
        result.Value.Description.Should().Be("New description");
        result.Value.Servings.Should().Be(6);
        result.Value.PrepTimeMinutes.Should().Be(15);
        result.Value.CookTimeMinutes.Should().Be(30);
        result.Value.Difficulty.Should().Be("hard");
        result.Value.ImageUrl.Should().Be("https://example.com/new.jpg");
        result.Value.Ingredients.Should().HaveCount(1);
        result.Value.Ingredients[0].Name.Should().Be("Butter");
        result.Value.Steps.Should().HaveCount(1);
        result.Value.Steps[0].Description.Should().Be("Melt butter");
    }

    [Fact]
    public async Task HandleAsync_ForwardsCancellationToken()
    {
        var recipe = CreateTestRecipe("user-123");
        var recipeId = recipe.Id;
        recipes.GetByIdAsync(recipeId, Arg.Any<CancellationToken>()).Returns(recipe);
        var cts = new CancellationTokenSource();

        var command = CreateValidCommand(recipeId);

        await handler.HandleAsync(command, cts.Token);

        await recipes.Received(1).GetByIdAsync(recipeId, cts.Token);
        await recipes.Received(1).UpdateAsync(recipe, cts.Token);
    }

    [Fact]
    public async Task HandleAsync_PreservesSourceUrl()
    {
        var recipe = CreateTestRecipeWithSourceUrl("user-123");
        var recipeId = recipe.Id;
        recipes.GetByIdAsync(recipeId, Arg.Any<CancellationToken>()).Returns(recipe);

        var command = CreateValidCommand(recipeId);

        var result = await handler.HandleAsync(command);

        result.Value.SourceUrl.Should().Be("https://example.com/recipe");
    }

    [Fact]
    public async Task HandleAsync_NullOptionalFields_ClearsExistingValues()
    {
        var recipe = Recipe.Create(
            RecipeTitle.From("Original"),
            OwnerIdentifier.From("user-123"),
            [Ingredient.Create(IngredientName.From("Flour"), null, null)],
            [Step.Create(StepNumber.From(1), StepDescription.From("Mix"))],
            RecipeDescription.From("Old description"),
            Servings.From(4),
            PreparationTime.From(10),
            CookingTime.From(20),
            Difficulty.From("easy"),
            ImageUrl.From("https://example.com/old.jpg"));

        var recipeId = recipe.Id;
        recipes.GetByIdAsync(recipeId, Arg.Any<CancellationToken>()).Returns(recipe);

        var command = new UpdateRecipeCommand(
            recipeId,
            RecipeTitle.From("Updated"),
            [new SaveRecipeIngredientItem(IngredientName.From("Butter"), null, null)],
            [new SaveRecipeStepItem(StepNumber.From(1), StepDescription.From("Melt"))]);

        var result = await handler.HandleAsync(command);

        result.Value.Description.Should().BeNull();
        result.Value.Servings.Should().BeNull();
        result.Value.PrepTimeMinutes.Should().BeNull();
        result.Value.CookTimeMinutes.Should().BeNull();
        result.Value.Difficulty.Should().BeNull();
        result.Value.ImageUrl.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_PreservesCreatedAt()
    {
        var recipe = CreateTestRecipe("user-123");
        var recipeId = recipe.Id;
        var originalCreatedAt = recipe.CreatedAt;
        recipes.GetByIdAsync(recipeId, Arg.Any<CancellationToken>()).Returns(recipe);

        var command = CreateValidCommand(recipeId);

        var result = await handler.HandleAsync(command);

        result.Value.CreatedAt.Should().Be(originalCreatedAt);
    }

    private static UpdateRecipeCommand CreateValidCommand(RecipeIdentifier identifier)
    {
        return new UpdateRecipeCommand(
            identifier,
            RecipeTitle.From("Updated Pasta"),
            [new SaveRecipeIngredientItem(IngredientName.From("Spaghetti"), Amount.From(400), Unit.From("g"))],
            [new SaveRecipeStepItem(StepNumber.From(1), StepDescription.From("Cook pasta"))]);
    }

    private static Recipe CreateTestRecipe(string ownerId)
    {
        return Recipe.Create(
            RecipeTitle.From("Test Recipe"),
            OwnerIdentifier.From(ownerId),
            [Ingredient.Create(IngredientName.From("Flour"), null, null)],
            [Step.Create(StepNumber.From(1), StepDescription.From("Mix"))]);
    }

    private static Recipe CreateTestRecipeWithSourceUrl(string ownerId)
    {
        return Recipe.Create(
            RecipeTitle.From("Test Recipe"),
            OwnerIdentifier.From(ownerId),
            [Ingredient.Create(IngredientName.From("Flour"), null, null)],
            [Step.Create(StepNumber.From(1), StepDescription.From("Mix"))],
            sourceUrl: RecipeUrl.From("https://example.com/recipe"));
    }
}
