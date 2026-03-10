using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using Yumney.Recipes.Application.Commands;
using Yumney.Recipes.Domain.Recipe;
using Yumney.Shared.Common;

namespace Yumney.Recipes.Application.Tests.Commands;

public class SaveRecipeCommandHandlerTests
{
    private readonly IRecipeRepository recipes = Substitute.For<IRecipeRepository>();
    private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
    private readonly ILogger<SaveRecipeCommandHandler> logger = Substitute.For<ILogger<SaveRecipeCommandHandler>>();
    private readonly SaveRecipeCommandHandler handler;

    public SaveRecipeCommandHandlerTests()
    {
        currentUser.UserId.Returns("user-123");
        handler = new SaveRecipeCommandHandler(recipes, currentUser, logger);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_ReturnsSuccess()
    {
        var command = CreateValidCommand();

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_ReturnsSavedRecipeDto()
    {
        var command = CreateValidCommand();

        var result = await handler.HandleAsync(command);

        result.Value.Title.Should().Be("Pasta Carbonara");
        result.Value.Identifier.Should().NotBeEmpty();
        result.Value.ImportedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_CallsAddAsync()
    {
        var command = CreateValidCommand();

        await handler.HandleAsync(command);

        await recipes.Received(1).AddAsync(Arg.Any<Recipe>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DuplicateUrl_ReturnsFailure()
    {
        var command = CreateValidCommand();
        recipes.ExistsBySourceUrlAsync(
            Arg.Any<RecipeUrl>(),
            Arg.Any<OwnerIdentifier>(),
            Arg.Any<CancellationToken>()).Returns(true);

        var result = await handler.HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SaveRecipeErrors.AlreadyImported);
    }

    [Fact]
    public async Task HandleAsync_DuplicateUrl_DoesNotCallAddAsync()
    {
        var command = CreateValidCommand();
        recipes.ExistsBySourceUrlAsync(
            Arg.Any<RecipeUrl>(),
            Arg.Any<OwnerIdentifier>(),
            Arg.Any<CancellationToken>()).Returns(true);

        await handler.HandleAsync(command);

        await recipes.DidNotReceive().AddAsync(Arg.Any<Recipe>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_UsesCurrentUserAsOwner()
    {
        currentUser.UserId.Returns("specific-user-id");
        var command = CreateValidCommand();

        await handler.HandleAsync(command);

        await recipes.Received(1).ExistsBySourceUrlAsync(
            Arg.Any<RecipeUrl>(),
            Arg.Is<OwnerIdentifier>(o => o.Value == "specific-user-id"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_CreatesRecipeWithCorrectIngredients()
    {
        Recipe? capturedRecipe = null;
        await recipes.AddAsync(Arg.Do<Recipe>(r => capturedRecipe = r), Arg.Any<CancellationToken>());

        var command = new SaveRecipeCommand(
            new RecipeTitle("Test"),
            new RecipeUrl("https://example.com/recipe"),
            [
                new SaveRecipeIngredientCommand(new IngredientName("Flour"), new Amount(500), new Unit("g")),
                new SaveRecipeIngredientCommand(new IngredientName("Sugar"), new Amount(100), null),
            ],
            [new SaveRecipeStepCommand(new StepNumber(1), new StepDescription("Mix"))]);

        await handler.HandleAsync(command);

        capturedRecipe.Should().NotBeNull();
        capturedRecipe!.Ingredients.Should().HaveCount(2);
        capturedRecipe.Ingredients[0].Name.Value.Should().Be("Flour");
        capturedRecipe.Ingredients[1].Name.Value.Should().Be("Sugar");
        capturedRecipe.Ingredients[1].Unit.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_CreatesRecipeWithCorrectSteps()
    {
        Recipe? capturedRecipe = null;
        await recipes.AddAsync(Arg.Do<Recipe>(r => capturedRecipe = r), Arg.Any<CancellationToken>());

        var command = new SaveRecipeCommand(
            new RecipeTitle("Test"),
            new RecipeUrl("https://example.com/recipe"),
            [new SaveRecipeIngredientCommand(new IngredientName("Flour"), null, null)],
            [
                new SaveRecipeStepCommand(new StepNumber(1), new StepDescription("Preheat oven")),
                new SaveRecipeStepCommand(new StepNumber(2), new StepDescription("Mix ingredients")),
            ]);

        await handler.HandleAsync(command);

        capturedRecipe.Should().NotBeNull();
        capturedRecipe!.Steps.Should().HaveCount(2);
        capturedRecipe.Steps[0].Description.Value.Should().Be("Preheat oven");
        capturedRecipe.Steps[1].Description.Value.Should().Be("Mix ingredients");
    }

    [Fact]
    public async Task HandleAsync_WithOptionalFields_PassesThemThrough()
    {
        Recipe? capturedRecipe = null;
        await recipes.AddAsync(Arg.Do<Recipe>(r => capturedRecipe = r), Arg.Any<CancellationToken>());

        var command = new SaveRecipeCommand(
            new RecipeTitle("Test"),
            new RecipeUrl("https://example.com/recipe"),
            [new SaveRecipeIngredientCommand(new IngredientName("Flour"), null, null)],
            [new SaveRecipeStepCommand(new StepNumber(1), new StepDescription("Mix"))],
            new RecipeDescription("A test recipe"),
            new Servings(4),
            new PreparationTime(10),
            new CookingTime(20),
            new Difficulty("easy"),
            new ImageUrl("https://example.com/image.jpg"));

        await handler.HandleAsync(command);

        capturedRecipe.Should().NotBeNull();
        capturedRecipe!.Description!.Value.Should().Be("A test recipe");
        capturedRecipe.Servings!.Value.Should().Be(4);
        capturedRecipe.PreparationTime!.Value.Should().Be(10);
        capturedRecipe.CookingTime!.Value.Should().Be(20);
        capturedRecipe.Difficulty!.Value.Should().Be("easy");
        capturedRecipe.ImageUrl!.Value.Should().Be("https://example.com/image.jpg");
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_ReturnsDtoWithMatchingIdentifier()
    {
        Recipe? capturedRecipe = null;
        await recipes.AddAsync(Arg.Do<Recipe>(r => capturedRecipe = r), Arg.Any<CancellationToken>());

        var command = CreateValidCommand();

        var result = await handler.HandleAsync(command);

        capturedRecipe.Should().NotBeNull();
        result.Value.Identifier.Should().Be(capturedRecipe!.Id);
    }

    [Fact]
    public async Task HandleAsync_ForwardsCancellationToken()
    {
        var cts = new CancellationTokenSource();
        var command = CreateValidCommand();

        await handler.HandleAsync(command, cts.Token);

        await recipes.Received(1).ExistsBySourceUrlAsync(
            Arg.Any<RecipeUrl>(),
            Arg.Any<OwnerIdentifier>(),
            cts.Token);

        await recipes.Received(1).AddAsync(
            Arg.Any<Recipe>(),
            cts.Token);
    }

    private static SaveRecipeCommand CreateValidCommand()
    {
        return new SaveRecipeCommand(
            new RecipeTitle("Pasta Carbonara"),
            new RecipeUrl("https://example.com/recipe"),
            [new SaveRecipeIngredientCommand(new IngredientName("Spaghetti"), new Amount(400), new Unit("g"))],
            [new SaveRecipeStepCommand(new StepNumber(1), new StepDescription("Cook pasta"))]);
    }
}
