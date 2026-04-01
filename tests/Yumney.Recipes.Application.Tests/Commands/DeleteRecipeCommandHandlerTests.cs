using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe.Events;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Tests.Commands;

public class DeleteRecipeCommandHandlerTests
{
    private readonly IRecipeRepository recipes = Substitute.For<IRecipeRepository>();
    private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
    private readonly ILogger<DeleteRecipeCommandHandler> logger = Substitute.For<ILogger<DeleteRecipeCommandHandler>>();
    private readonly DeleteRecipeCommandHandler handler;

    public DeleteRecipeCommandHandlerTests()
    {
        currentUser.UserId.Returns("user-123");
        handler = new DeleteRecipeCommandHandler(recipes, currentUser, logger);
    }

    [Fact]
    public async Task HandleAsync_ExistingRecipe_ReturnsSuccess()
    {
        var recipe = CreateTestRecipe("user-123");
        var recipeId = recipe.Id;
        recipes.GetByIdAsync(recipeId, Arg.Any<CancellationToken>()).Returns(recipe);

        var command = new DeleteRecipeCommand(recipeId);

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_RecipeNotFound_ReturnsFailure()
    {
        var recipeId = RecipeIdentifier.New();
        recipes.GetByIdAsync(recipeId, Arg.Any<CancellationToken>()).Returns((Recipe?)null);

        var command = new DeleteRecipeCommand(recipeId);

        var result = await handler.HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DeleteRecipeErrors.NotFound);
    }

    [Fact]
    public async Task HandleAsync_WrongOwner_ReturnsAccessDenied()
    {
        var recipe = CreateTestRecipe("other-user");
        var recipeId = recipe.Id;
        recipes.GetByIdAsync(recipeId, Arg.Any<CancellationToken>()).Returns(recipe);

        var command = new DeleteRecipeCommand(recipeId);

        var result = await handler.HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DeleteRecipeErrors.AccessDenied);
    }

    [Fact]
    public async Task HandleAsync_ExistingRecipe_CallsMarkAsDeleted()
    {
        var recipe = CreateTestRecipe("user-123");
        var recipeId = recipe.Id;
        recipes.GetByIdAsync(recipeId, Arg.Any<CancellationToken>()).Returns(recipe);

        var command = new DeleteRecipeCommand(recipeId);

        await handler.HandleAsync(command);

        recipe.DomainEvents.Should().ContainSingle(e => e is RecipeDeletedEvent);
    }

    [Fact]
    public async Task HandleAsync_ExistingRecipe_CallsDeleteAsync()
    {
        var recipe = CreateTestRecipe("user-123");
        var recipeId = recipe.Id;
        recipes.GetByIdAsync(recipeId, Arg.Any<CancellationToken>()).Returns(recipe);

        var command = new DeleteRecipeCommand(recipeId);

        await handler.HandleAsync(command);

        await recipes.Received(1).DeleteAsync(recipe, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_RecipeNotFound_DoesNotCallDeleteAsync()
    {
        var recipeId = RecipeIdentifier.New();
        recipes.GetByIdAsync(recipeId, Arg.Any<CancellationToken>()).Returns((Recipe?)null);

        var command = new DeleteRecipeCommand(recipeId);

        await handler.HandleAsync(command);

        await recipes.DidNotReceive().DeleteAsync(Arg.Any<Recipe>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WrongOwner_DoesNotCallDeleteAsync()
    {
        var recipe = CreateTestRecipe("other-user");
        var recipeId = recipe.Id;
        recipes.GetByIdAsync(recipeId, Arg.Any<CancellationToken>()).Returns(recipe);

        var command = new DeleteRecipeCommand(recipeId);

        await handler.HandleAsync(command);

        await recipes.DidNotReceive().DeleteAsync(Arg.Any<Recipe>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ForwardsCancellationToken()
    {
        var recipe = CreateTestRecipe("user-123");
        var recipeId = recipe.Id;
        recipes.GetByIdAsync(recipeId, Arg.Any<CancellationToken>()).Returns(recipe);
        var cts = new CancellationTokenSource();

        var command = new DeleteRecipeCommand(recipeId);

        await handler.HandleAsync(command, cts.Token);

        await recipes.Received(1).GetByIdAsync(recipeId, cts.Token);
        await recipes.Received(1).DeleteAsync(recipe, cts.Token);
    }

    private static Recipe CreateTestRecipe(string ownerId)
    {
        return Recipe.Create(
            RecipeTitle.From("Test Recipe"),
            OwnerIdentifier.From(ownerId),
            [Ingredient.Create(IngredientName.From("Flour"), null)],
            [Step.Create(StepNumber.From(1), StepDescription.From("Mix"))]);
    }
}
