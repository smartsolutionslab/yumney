using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Tests.Commands;

public class RateRecipeCommandHandlerTests
{
	private readonly IRecipeRepository recipes = Substitute.For<IRecipeRepository>();
	private readonly IRecipesUnitOfWork unitOfWork = Substitute.For<IRecipesUnitOfWork>();
	private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
	private readonly RateRecipeCommandHandler handler;

	public RateRecipeCommandHandlerTests()
	{
		currentUser.UserId.Returns("user-123");
		unitOfWork.Recipes.Returns(recipes);
		handler = new RateRecipeCommandHandler(unitOfWork, currentUser);
	}

	[Fact]
	public async Task HandleAsync_ValidCommand_StoresTheRating()
	{
		var recipe = RecipeTestData.CreateRecipe();
		recipes.GetByIdForUpdateAsync(recipe.Id, Arg.Any<CancellationToken>()).Returns(recipe);

		var result = await handler.HandleAsync(new RateRecipeCommand(recipe.Id, Rating.From(4)));

		result.IsSuccess.Should().BeTrue();
		recipe.Rating!.Value.Should().Be(4);
		await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_DifferentOwner_ReturnsAccessDenied()
	{
		var recipe = RecipeTestData.CreateRecipe(ownerId: "other-user");
		recipes.GetByIdForUpdateAsync(recipe.Id, Arg.Any<CancellationToken>()).Returns(recipe);

		var result = await handler.HandleAsync(new RateRecipeCommand(recipe.Id, Rating.From(5)));

		result.IsSuccess.Should().BeFalse();
		result.Error.Should().Be(RateRecipeErrors.AccessDenied);
		await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
	}
}
