using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Queries;
using SmartSolutionsLab.Yumney.Recipes.Application.Queries.Handlers;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Tests.Queries;

public class GetRecipeByIdQueryHandlerTests
{
    private readonly IRecipeRepository recipes = Substitute.For<IRecipeRepository>();
    private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
    private readonly ILogger<GetRecipeByIdQueryHandler> logger = Substitute.For<ILogger<GetRecipeByIdQueryHandler>>();
    private readonly GetRecipeByIdQueryHandler handler;

    public GetRecipeByIdQueryHandlerTests()
    {
        currentUser.UserId.Returns("user-123");
        handler = new GetRecipeByIdQueryHandler(recipes, currentUser, logger);
    }

    [Fact]
    public async Task HandleAsync_RecipeExists_ReturnsSuccessResult()
    {
        var recipe = RecipeTestData.CreateRecipe();
        var recipeId = recipe.Id;
        recipes.GetByIdAsync(recipeId, Arg.Any<CancellationToken>()).Returns(recipe);

        var result = await handler.HandleAsync(new(recipeId));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_RecipeExists_ReturnsMappedTitle()
    {
        var recipe = RecipeTestData.CreateRecipe(title: "Pasta Carbonara");
        var recipeId = recipe.Id;
        recipes.GetByIdAsync(recipeId, Arg.Any<CancellationToken>()).Returns(recipe);

        var result = await handler.HandleAsync(new(recipeId));

        result.Value.Title.Should().Be("Pasta Carbonara");
    }

    [Fact]
    public async Task HandleAsync_RecipeExists_ReturnsMappedIdentifier()
    {
        var recipe = RecipeTestData.CreateRecipe();
        var recipeId = recipe.Id;
        recipes.GetByIdAsync(recipeId, Arg.Any<CancellationToken>()).Returns(recipe);

        var result = await handler.HandleAsync(new(recipeId));

        result.Value.Identifier.Should().Be(recipe.Id.Value);
    }

    [Fact]
    public async Task HandleAsync_RecipeNotFound_ReturnsFailure()
    {
        var id = Guid.NewGuid();
        var recipeId = RecipeIdentifier.From(id);
        recipes.GetByIdAsync(RecipeIdentifier.From(id), Arg.Any<CancellationToken>()).Returns((Recipe?)null);

        var result = await handler.HandleAsync(new(recipeId));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(GetRecipeByIdErrors.NotFound);
    }

    [Fact]
    public async Task HandleAsync_WrongOwner_ReturnsAccessDeniedFailure()
    {
        var recipe = RecipeTestData.CreateRecipe("other-user");
        var recipeId = recipe.Id;
        recipes.GetByIdAsync(recipeId, Arg.Any<CancellationToken>()).Returns(recipe);

        var result = await handler.HandleAsync(new(recipeId));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(GetRecipeByIdErrors.AccessDenied);
    }

    [Fact]
    public async Task HandleAsync_RecipeWithAllFields_MapsOptionalFieldsCorrectly()
    {
        var recipe = RecipeTestData.CreateRecipeWithOptionals();
        var recipeId = recipe.Id;
        recipes.GetByIdAsync(recipeId, Arg.Any<CancellationToken>()).Returns(recipe);

        var result = await handler.HandleAsync(new(recipeId));

        var dto = result.Value;
        dto.Description.Should().Be("A test recipe");
        dto.Servings.Should().Be(4);
        dto.PrepTimeMinutes.Should().Be(10);
        dto.CookTimeMinutes.Should().Be(20);
        dto.Difficulty.Should().Be("easy");
        dto.ImageUrl.Should().Be("https://example.com/image.jpg");
        dto.SourceUrl.Should().Be("https://example.com/recipe");
    }

    [Fact]
    public async Task HandleAsync_RecipeWithoutOptionals_MapsNullFields()
    {
        var recipe = RecipeTestData.CreateRecipe();
        var recipeId = recipe.Id;
        recipes.GetByIdAsync(recipeId, Arg.Any<CancellationToken>()).Returns(recipe);

        var result = await handler.HandleAsync(new(recipeId));

        var dto = result.Value;
        dto.Description.Should().BeNull();
        dto.Servings.Should().BeNull();
        dto.PrepTimeMinutes.Should().BeNull();
        dto.CookTimeMinutes.Should().BeNull();
        dto.Difficulty.Should().BeNull();
        dto.ImageUrl.Should().BeNull();
        dto.SourceUrl.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_RecipeWithIngredients_MapsIngredientsCorrectly()
    {
        var recipe = RecipeTestData.CreateRecipeWithIngredients();
        var recipeId = recipe.Id;
        recipes.GetByIdAsync(recipeId, Arg.Any<CancellationToken>()).Returns(recipe);

        var result = await handler.HandleAsync(new(recipeId));

        result.Value.Ingredients.Should().HaveCount(2);
        result.Value.Ingredients[0].Name.Should().Be("Flour");
        result.Value.Ingredients[0].Amount.Should().Be(500m);
        result.Value.Ingredients[0].Unit.Should().Be("g");
        result.Value.Ingredients[1].Name.Should().Be("Eggs");
        result.Value.Ingredients[1].Amount.Should().BeNull();
        result.Value.Ingredients[1].Unit.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_RecipeWithSteps_MapsStepsCorrectly()
    {
        var recipe = RecipeTestData.CreateRecipeWithSteps();
        var recipeId = recipe.Id;
        recipes.GetByIdAsync(recipeId, Arg.Any<CancellationToken>()).Returns(recipe);

        var result = await handler.HandleAsync(new(recipeId));

        result.Value.Steps.Should().HaveCount(2);
        result.Value.Steps[0].Number.Should().Be(1);
        result.Value.Steps[0].Description.Should().Be("Mix flour");
        result.Value.Steps[1].Number.Should().Be(2);
        result.Value.Steps[1].Description.Should().Be("Add eggs");
    }

    [Fact]
    public async Task HandleAsync_Always_CallsRepositoryWithCorrectIdentifier()
    {
        var recipe = RecipeTestData.CreateRecipe();
        var recipeId = recipe.Id;
        recipes.GetByIdAsync(recipeId, Arg.Any<CancellationToken>()).Returns(recipe);

        await handler.HandleAsync(new(recipeId));

        await recipes.Received(1).GetByIdAsync(recipeId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_RecipeExists_MapsCreatedAt()
    {
        var recipe = RecipeTestData.CreateRecipe();
        var recipeId = recipe.Id;
        recipes.GetByIdAsync(recipeId, Arg.Any<CancellationToken>()).Returns(recipe);

        var result = await handler.HandleAsync(new(recipeId));

        result.Value.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task HandleAsync_WithCancellationToken_ForwardsToRepository()
    {
        var id = Guid.NewGuid();
        var recipeId = RecipeIdentifier.From(id);
        var cts = new CancellationTokenSource();
        recipes.GetByIdAsync(recipeId, Arg.Any<CancellationToken>()).Returns((Recipe?)null);

        await handler.HandleAsync(new(recipeId), cts.Token);

        await recipes.Received(1).GetByIdAsync(recipeId, cts.Token);
    }
}
