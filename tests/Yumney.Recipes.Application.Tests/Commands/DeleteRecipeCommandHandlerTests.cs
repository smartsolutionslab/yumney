using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe.Events;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Events;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Tests.Commands;

public class DeleteRecipeCommandHandlerTests
{
	private readonly IRecipeRepository recipes = Substitute.For<IRecipeRepository>();
	private readonly IRecipesUnitOfWork unitOfWork = Substitute.For<IRecipesUnitOfWork>();
	private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
	private readonly IEventBus eventBus = Substitute.For<IEventBus>();
	private readonly DeleteRecipeCommandHandler handler;

	public DeleteRecipeCommandHandlerTests()
	{
		currentUser.UserId.Returns("user-123");
		unitOfWork.Recipes.Returns(recipes);
		handler = new DeleteRecipeCommandHandler(unitOfWork, currentUser, eventBus);
	}

	[Fact]
	public async Task HandleAsync_ExistingRecipe_ReturnsSuccess()
	{
		var recipe = RecipeTestData.CreateRecipe();
		var recipeId = recipe.Id;
		recipes.GetByIdForUpdateAsync(recipeId, Arg.Any<CancellationToken>()).Returns(recipe);

		var command = new DeleteRecipeCommand(recipeId);

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();
	}

	[Fact]
	public async Task HandleAsync_RecipeNotFound_ThrowsEntityNotFoundException()
	{
		var recipeId = RecipeIdentifier.New();
		recipes.GetByIdForUpdateAsync(recipeId, Arg.Any<CancellationToken>())
			.Returns<Recipe>(_ => throw new EntityNotFoundException(nameof(Recipe), recipeId.Value));

		var command = new DeleteRecipeCommand(recipeId);

		var act = () => handler.HandleAsync(command);

		await act.Should().ThrowAsync<EntityNotFoundException>();
	}

	[Fact]
	public async Task HandleAsync_WrongOwner_ReturnsAccessDenied()
	{
		var recipe = RecipeTestData.CreateRecipe("other-user");
		var recipeId = recipe.Id;
		recipes.GetByIdForUpdateAsync(recipeId, Arg.Any<CancellationToken>()).Returns(recipe);

		var command = new DeleteRecipeCommand(recipeId);

		var result = await handler.HandleAsync(command);

		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(DeleteRecipeErrors.AccessDenied);
	}

	[Fact]
	public async Task HandleAsync_ExistingRecipe_CallsMarkAsDeleted()
	{
		var recipe = RecipeTestData.CreateRecipe();
		var recipeId = recipe.Id;
		recipes.GetByIdForUpdateAsync(recipeId, Arg.Any<CancellationToken>()).Returns(recipe);

		var command = new DeleteRecipeCommand(recipeId);

		await handler.HandleAsync(command);

		recipe.DomainEvents.Should().ContainSingle(e => e is RecipeDeletedEvent);
	}

	[Fact]
	public async Task HandleAsync_ExistingRecipe_CallsDeleteAsync()
	{
		var recipe = RecipeTestData.CreateRecipe();
		var recipeId = recipe.Id;
		recipes.GetByIdForUpdateAsync(recipeId, Arg.Any<CancellationToken>()).Returns(recipe);

		var command = new DeleteRecipeCommand(recipeId);

		await handler.HandleAsync(command);

		recipes.Received(1).Remove(recipe);
		await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_RecipeNotFound_DoesNotCallDeleteAsync()
	{
		var recipeId = RecipeIdentifier.New();
		recipes.GetByIdForUpdateAsync(recipeId, Arg.Any<CancellationToken>())
			.Returns<Recipe>(_ => throw new EntityNotFoundException(nameof(Recipe), recipeId.Value));

		var command = new DeleteRecipeCommand(recipeId);

		try { await handler.HandleAsync(command); } catch (EntityNotFoundException) { }

		recipes.DidNotReceive().Remove(Arg.Any<Recipe>());
		await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_WrongOwner_DoesNotCallDeleteAsync()
	{
		var recipe = RecipeTestData.CreateRecipe("other-user");
		var recipeId = recipe.Id;
		recipes.GetByIdForUpdateAsync(recipeId, Arg.Any<CancellationToken>()).Returns(recipe);

		var command = new DeleteRecipeCommand(recipeId);

		await handler.HandleAsync(command);

		recipes.DidNotReceive().Remove(Arg.Any<Recipe>());
		await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_ForwardsCancellationToken()
	{
		var recipe = RecipeTestData.CreateRecipe();
		var recipeId = recipe.Id;
		recipes.GetByIdForUpdateAsync(recipeId, Arg.Any<CancellationToken>()).Returns(recipe);
		var cts = new CancellationTokenSource();

		var command = new DeleteRecipeCommand(recipeId);

		await handler.HandleAsync(command, cts.Token);

		await recipes.Received(1).GetByIdForUpdateAsync(recipeId, cts.Token);
		recipes.Received(1).Remove(recipe);
		await unitOfWork.Received(1).SaveChangesAsync(cts.Token);
	}
}
