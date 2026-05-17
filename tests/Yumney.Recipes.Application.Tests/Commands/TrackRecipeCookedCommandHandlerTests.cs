using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.Contracts;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Tests.Commands;

public class TrackRecipeCookedCommandHandlerTests
{
	private readonly IRecipeRepository recipes = Substitute.For<IRecipeRepository>();
	private readonly IRecipesUnitOfWork unitOfWork = Substitute.For<IRecipesUnitOfWork>();
	private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
	private readonly IEventBus eventBus = Substitute.For<IEventBus>();
	private readonly TrackRecipeCookedCommandHandler handler;

	public TrackRecipeCookedCommandHandlerTests()
	{
		currentUser.UserId.Returns("user-123");
		unitOfWork.Recipes.Returns(recipes);
		handler = new TrackRecipeCookedCommandHandler(unitOfWork, currentUser, eventBus);
	}

	[Fact]
	public async Task HandleAsync_OwnerMatches_PublishesEventAndSaves()
	{
		var recipe = RecipeTestData.CreateRecipe();
		recipes.GetByIdAsync(recipe.Id, Arg.Any<CancellationToken>()).Returns(recipe);

		var result = await handler.HandleAsync(new TrackRecipeCookedCommand(recipe.Id));

		result.IsSuccess.Should().BeTrue();
		await eventBus.Received(1).PublishAsync(
			Arg.Is<RecipeCookedIntegrationEvent>(@event =>
				@event.OwnerId == "user-123"
				&& @event.RecipeIdentifier == recipe.Id.Value
				&& @event.RecipeTitle == recipe.Title.Value),
			Arg.Any<CancellationToken>());
		await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_DifferentOwner_ReturnsAccessDenied()
	{
		var recipe = RecipeTestData.CreateRecipe(ownerId: "other-user");
		recipes.GetByIdAsync(recipe.Id, Arg.Any<CancellationToken>()).Returns(recipe);

		var result = await handler.HandleAsync(new TrackRecipeCookedCommand(recipe.Id));

		result.IsSuccess.Should().BeFalse();
		result.Error.Should().Be(TrackRecipeCookedErrors.AccessDenied);
		await eventBus.DidNotReceiveWithAnyArgs().PublishAsync<RecipeCookedIntegrationEvent>(default!, default);
		await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
	}
}
