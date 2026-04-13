using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Domain.RecipeFavorite;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Tests.Commands;

public class ToggleFavoriteCommandHandlerTests
{
    private readonly IRecipeRepository recipes = Substitute.For<IRecipeRepository>();
    private readonly IRecipeFavoriteRepository favorites = Substitute.For<IRecipeFavoriteRepository>();
    private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
    private readonly ToggleFavoriteCommandHandler handler;

    public ToggleFavoriteCommandHandlerTests()
    {
        currentUser.UserId.Returns("user-123");
        handler = new ToggleFavoriteCommandHandler(recipes, favorites, currentUser);
    }

    [Fact]
    public async Task HandleAsync_NotYetFavorited_ReturnsIsFavoriteTrue()
    {
        var recipe = RecipeTestData.CreateRecipe();
        recipes.GetByIdAsync(recipe.Id, Arg.Any<CancellationToken>()).Returns(recipe);
        favorites
            .IsFavoritedAsync(Arg.Any<OwnerIdentifier>(), recipe.Id, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await handler.HandleAsync(new ToggleFavoriteCommand(recipe.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.IsFavorite.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_NotYetFavorited_AddsFavorite()
    {
        var recipe = RecipeTestData.CreateRecipe();
        recipes.GetByIdAsync(recipe.Id, Arg.Any<CancellationToken>()).Returns(recipe);
        favorites
            .IsFavoritedAsync(Arg.Any<OwnerIdentifier>(), recipe.Id, Arg.Any<CancellationToken>())
            .Returns(false);

        await handler.HandleAsync(new ToggleFavoriteCommand(recipe.Id));

        await favorites.Received(1).AddAsync(
            Arg.Is<RecipeFavorite>(f => f.RecipeIdentifier == recipe.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AlreadyFavorited_ReturnsIsFavoriteFalse()
    {
        var recipe = RecipeTestData.CreateRecipe();
        recipes.GetByIdAsync(recipe.Id, Arg.Any<CancellationToken>()).Returns(recipe);
        favorites
            .IsFavoritedAsync(Arg.Any<OwnerIdentifier>(), recipe.Id, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await handler.HandleAsync(new ToggleFavoriteCommand(recipe.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.IsFavorite.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_AlreadyFavorited_RemovesFavorite()
    {
        var recipe = RecipeTestData.CreateRecipe();
        recipes.GetByIdAsync(recipe.Id, Arg.Any<CancellationToken>()).Returns(recipe);
        favorites
            .IsFavoritedAsync(Arg.Any<OwnerIdentifier>(), recipe.Id, Arg.Any<CancellationToken>())
            .Returns(true);

        await handler.HandleAsync(new ToggleFavoriteCommand(recipe.Id));

        await favorites.Received(1).RemoveAsync(
            Arg.Any<OwnerIdentifier>(),
            recipe.Id,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_RecipeNotFound_ThrowsEntityNotFoundException()
    {
        var recipeId = RecipeIdentifier.New();
        recipes.GetByIdAsync(recipeId, Arg.Any<CancellationToken>())
            .Returns<Recipe>(_ => throw new EntityNotFoundException(nameof(Recipe), recipeId.Value));

        var act = () => handler.HandleAsync(new ToggleFavoriteCommand(recipeId));

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task HandleAsync_WrongOwner_ReturnsAccessDenied()
    {
        var recipe = RecipeTestData.CreateRecipe("other-user");
        recipes.GetByIdAsync(recipe.Id, Arg.Any<CancellationToken>()).Returns(recipe);

        var result = await handler.HandleAsync(new ToggleFavoriteCommand(recipe.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ToggleFavoriteErrors.AccessDenied);
    }

    [Fact]
    public async Task HandleAsync_WrongOwner_DoesNotMutateFavorites()
    {
        var recipe = RecipeTestData.CreateRecipe("other-user");
        recipes.GetByIdAsync(recipe.Id, Arg.Any<CancellationToken>()).Returns(recipe);

        await handler.HandleAsync(new ToggleFavoriteCommand(recipe.Id));

        await favorites.DidNotReceive().AddAsync(Arg.Any<RecipeFavorite>(), Arg.Any<CancellationToken>());
        await favorites.DidNotReceive().RemoveAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<RecipeIdentifier>(), Arg.Any<CancellationToken>());
    }
}
