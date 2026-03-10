using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using Yumney.Recipes.Application.DTOs;
using Yumney.Recipes.Application.Queries;
using Yumney.Recipes.Domain.Recipe;
using Yumney.Shared.Common;

namespace Yumney.Recipes.Application.Tests.Queries;

public class GetRecipesQueryHandlerTests
{
    private readonly IRecipeRepository recipes = Substitute.For<IRecipeRepository>();
    private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
    private readonly ILogger<GetRecipesQueryHandler> logger = Substitute.For<ILogger<GetRecipesQueryHandler>>();
    private readonly GetRecipesQueryHandler handler;

    public GetRecipesQueryHandlerTests()
    {
        currentUser.UserId.Returns("user-123");
        handler = new GetRecipesQueryHandler(recipes, currentUser, logger);
    }

    [Fact]
    public async Task HandleAsync_ReturnsSuccess()
    {
        SetupEmptyRepository();

        var query = new GetRecipesQuery(1, 20, RecipeSortField.Date, SortDirection.Descending);
        var result = await handler.HandleAsync(query);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_EmptyList_ReturnsEmptyItems()
    {
        SetupEmptyRepository();

        var query = new GetRecipesQuery(1, 20, RecipeSortField.Date, SortDirection.Descending);
        var result = await handler.HandleAsync(query);

        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_WithRecipes_ReturnsMappedDtos()
    {
        var recipe = CreateTestRecipe("Pasta Carbonara");
        SetupRepository([recipe], 1);

        var query = new GetRecipesQuery(1, 20, RecipeSortField.Date, SortDirection.Descending);
        var result = await handler.HandleAsync(query);

        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].Title.Should().Be("Pasta Carbonara");
        result.Value.Items[0].Identifier.Should().Be(recipe.Id);
    }

    [Fact]
    public async Task HandleAsync_ReturnsPaginationInfo()
    {
        SetupRepository([], 50);

        var query = new GetRecipesQuery(3, 10, RecipeSortField.Date, SortDirection.Descending);
        var result = await handler.HandleAsync(query);

        result.Value.Page.Should().Be(3);
        result.Value.PageSize.Should().Be(10);
        result.Value.TotalCount.Should().Be(50);
    }

    [Fact]
    public async Task HandleAsync_UsesCurrentUserAsOwner()
    {
        currentUser.UserId.Returns("specific-user-id");
        SetupEmptyRepository();

        var query = new GetRecipesQuery(1, 20, RecipeSortField.Date, SortDirection.Descending);
        await handler.HandleAsync(query);

        await recipes.Received(1).GetByOwnerAsync(
            Arg.Is<OwnerIdentifier>(o => o.Value == "specific-user-id"),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<RecipeSortField>(),
            Arg.Any<SortDirection>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_CalculatesSkipFromPage()
    {
        SetupEmptyRepository();

        var query = new GetRecipesQuery(3, 10, RecipeSortField.Date, SortDirection.Descending);
        await handler.HandleAsync(query);

        await recipes.Received(1).GetByOwnerAsync(
            Arg.Any<OwnerIdentifier>(),
            20,
            10,
            Arg.Any<RecipeSortField>(),
            Arg.Any<SortDirection>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_PassesSortParameters()
    {
        SetupEmptyRepository();

        var query = new GetRecipesQuery(1, 20, RecipeSortField.Name, SortDirection.Ascending);
        await handler.HandleAsync(query);

        await recipes.Received(1).GetByOwnerAsync(
            Arg.Any<OwnerIdentifier>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            RecipeSortField.Name,
            SortDirection.Ascending,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ForwardsCancellationToken()
    {
        SetupEmptyRepository();
        var cts = new CancellationTokenSource();

        var query = new GetRecipesQuery(1, 20, RecipeSortField.Date, SortDirection.Descending);
        await handler.HandleAsync(query, cts.Token);

        await recipes.Received(1).GetByOwnerAsync(
            Arg.Any<OwnerIdentifier>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<RecipeSortField>(),
            Arg.Any<SortDirection>(),
            cts.Token);
    }

    [Fact]
    public async Task HandleAsync_MapsOptionalFields()
    {
        var recipe = CreateTestRecipeWithOptionals();
        SetupRepository([recipe], 1);

        var query = new GetRecipesQuery(1, 20, RecipeSortField.Date, SortDirection.Descending);
        var result = await handler.HandleAsync(query);

        var item = result.Value.Items[0];
        item.Description.Should().Be("A test recipe");
        item.Servings.Should().Be(4);
        item.PrepTimeMinutes.Should().Be(10);
        item.CookTimeMinutes.Should().Be(20);
        item.Difficulty.Should().Be("easy");
        item.ImageUrl.Should().Be("https://example.com/image.jpg");
    }

    [Fact]
    public async Task HandleAsync_NullOptionalFields_MapsToNull()
    {
        var recipe = CreateTestRecipe("Simple Recipe");
        SetupRepository([recipe], 1);

        var query = new GetRecipesQuery(1, 20, RecipeSortField.Date, SortDirection.Descending);
        var result = await handler.HandleAsync(query);

        var item = result.Value.Items[0];
        item.Description.Should().BeNull();
        item.Servings.Should().BeNull();
        item.PrepTimeMinutes.Should().BeNull();
        item.CookTimeMinutes.Should().BeNull();
        item.Difficulty.Should().BeNull();
        item.ImageUrl.Should().BeNull();
    }

    private static Recipe CreateTestRecipe(string title)
    {
        return Recipe.Create(
            new RecipeTitle(title),
            new OwnerIdentifier("user-123"),
            [Ingredient.Create(new IngredientName("Flour"), null, null)],
            [Step.Create(new StepNumber(1), new StepDescription("Mix"))]);
    }

    private static Recipe CreateTestRecipeWithOptionals()
    {
        return Recipe.Create(
            new RecipeTitle("Full Recipe"),
            new OwnerIdentifier("user-123"),
            [Ingredient.Create(new IngredientName("Flour"), null, null)],
            [Step.Create(new StepNumber(1), new StepDescription("Mix"))],
            new RecipeDescription("A test recipe"),
            new Servings(4),
            new PreparationTime(10),
            new CookingTime(20),
            new Difficulty("easy"),
            new ImageUrl("https://example.com/image.jpg"));
    }

    private void SetupEmptyRepository()
    {
        SetupRepository([], 0);
    }

    private void SetupRepository(IReadOnlyList<Recipe> items, int totalCount)
    {
        recipes.GetByOwnerAsync(
            Arg.Any<OwnerIdentifier>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<RecipeSortField>(),
            Arg.Any<SortDirection>(),
            Arg.Any<CancellationToken>())
            .Returns((items, totalCount));
    }
}
