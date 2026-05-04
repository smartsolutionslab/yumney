using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Tests.Commands;

public class UpdateRecipeNotesCommandHandlerTests
{
	private readonly IRecipeRepository recipes = Substitute.For<IRecipeRepository>();
	private readonly IRecipesUnitOfWork unitOfWork = Substitute.For<IRecipesUnitOfWork>();
	private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
	private readonly UpdateRecipeNotesCommandHandler handler;

	public UpdateRecipeNotesCommandHandlerTests()
	{
		currentUser.UserId.Returns("user-123");
		unitOfWork.Recipes.Returns(recipes);
		handler = new UpdateRecipeNotesCommandHandler(unitOfWork, currentUser);
	}

	[Fact]
	public async Task HandleAsync_ValidNotes_StoresTheNotes()
	{
		var recipe = RecipeTestData.CreateRecipe();
		recipes.GetByIdForUpdateAsync(recipe.Id, Arg.Any<CancellationToken>()).Returns(recipe);

		var result = await handler.HandleAsync(
			new UpdateRecipeNotesCommand(recipe.Id, Notes.From("Used less salt")));

		result.IsSuccess.Should().BeTrue();
		recipe.Notes!.Value.Should().Be("Used less salt");
	}

	[Fact]
	public async Task HandleAsync_NullNotes_ClearsExistingNotes()
	{
		var recipe = RecipeTestData.CreateRecipe();
		recipe.UpdateNotes(Notes.From("Initial"));
		recipes.GetByIdForUpdateAsync(recipe.Id, Arg.Any<CancellationToken>()).Returns(recipe);

		var result = await handler.HandleAsync(new UpdateRecipeNotesCommand(recipe.Id, null));

		result.IsSuccess.Should().BeTrue();
		recipe.Notes.Should().BeNull();
	}

	[Fact]
	public async Task HandleAsync_DifferentOwner_ReturnsAccessDenied()
	{
		var recipe = RecipeTestData.CreateRecipe(ownerId: "other-user");
		recipes.GetByIdForUpdateAsync(recipe.Id, Arg.Any<CancellationToken>()).Returns(recipe);

		var result = await handler.HandleAsync(
			new UpdateRecipeNotesCommand(recipe.Id, Notes.From("Should not save")));

		result.IsSuccess.Should().BeFalse();
		result.Error.Should().Be(RateRecipeErrors.AccessDenied);
		await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
	}
}
