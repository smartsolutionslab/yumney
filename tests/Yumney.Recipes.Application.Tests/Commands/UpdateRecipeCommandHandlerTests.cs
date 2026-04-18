using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Tests.Commands;

public class UpdateRecipeCommandHandlerTests
{
	private readonly IRecipeRepository recipes = Substitute.For<IRecipeRepository>();
	private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
	private readonly UpdateRecipeCommandHandler handler;

	public UpdateRecipeCommandHandlerTests()
	{
		currentUser.UserId.Returns("user-123");
		handler = new UpdateRecipeCommandHandler(recipes, currentUser);
	}

	[Fact]
	public async Task HandleAsync_ValidCommand_ReturnsSuccess()
	{
		var recipe = RecipeTestData.CreateRecipe();
		recipes.GetByIdForUpdateAsync(recipe.Id, Arg.Any<CancellationToken>()).Returns(recipe);

		var command = CreateValidCommand(recipe.Id);

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();
	}

	[Fact]
	public async Task HandleAsync_ValidCommand_ReturnsDtoWithMatchingIdentifier()
	{
		var recipe = RecipeTestData.CreateRecipe();
		recipes.GetByIdForUpdateAsync(recipe.Id, Arg.Any<CancellationToken>()).Returns(recipe);
		var command = CreateValidCommand(recipe.Id);

		var result = await handler.HandleAsync(command);

		result.Value.Identifier.Should().Be(recipe.Id.Value);
	}

	[Fact]
	public async Task HandleAsync_ValidCommand_ReturnsDtoWithUpdatedTitle()
	{
		var recipe = RecipeTestData.CreateRecipe();
		recipes.GetByIdForUpdateAsync(recipe.Id, Arg.Any<CancellationToken>()).Returns(recipe);
		var command = CreateValidCommand(recipe.Id);

		var result = await handler.HandleAsync(command);

		result.Value.Title.Should().Be("Updated Pasta");
	}

	[Fact]
	public async Task HandleAsync_ValidCommand_ReturnsDtoWithUpdatedIngredients()
	{
		var recipe = RecipeTestData.CreateRecipe();
		recipes.GetByIdForUpdateAsync(recipe.Id, Arg.Any<CancellationToken>()).Returns(recipe);
		var command = CreateValidCommand(recipe.Id);

		var result = await handler.HandleAsync(command);

		result.Value.Ingredients.Should().HaveCount(1);
	}

	[Fact]
	public async Task HandleAsync_ValidCommand_ReturnsDtoWithUpdatedSteps()
	{
		var recipe = RecipeTestData.CreateRecipe();
		recipes.GetByIdForUpdateAsync(recipe.Id, Arg.Any<CancellationToken>()).Returns(recipe);
		var command = CreateValidCommand(recipe.Id);

		var result = await handler.HandleAsync(command);

		result.Value.Steps.Should().HaveCount(1);
	}

	[Fact]
	public async Task HandleAsync_RecipeNotFound_ThrowsEntityNotFoundException()
	{
		var recipeId = RecipeIdentifier.New();
		recipes.GetByIdForUpdateAsync(recipeId, Arg.Any<CancellationToken>())
			.Returns<Recipe>(_ => throw new EntityNotFoundException(nameof(Recipe), recipeId.Value));

		var command = CreateValidCommand(recipeId);

		var act = () => handler.HandleAsync(command);

		await act.Should().ThrowAsync<EntityNotFoundException>();
	}

	[Fact]
	public async Task HandleAsync_WrongOwner_ReturnsAccessDenied()
	{
		var recipe = RecipeTestData.CreateRecipe("other-user");
		recipes.GetByIdForUpdateAsync(recipe.Id, Arg.Any<CancellationToken>()).Returns(recipe);

		var command = CreateValidCommand(recipe.Id);

		var result = await handler.HandleAsync(command);

		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(UpdateRecipeErrors.AccessDenied);
	}

	[Fact]
	public async Task HandleAsync_RecipeNotFound_DoesNotCallUpdateAsync()
	{
		var recipeId = RecipeIdentifier.New();
		recipes.GetByIdForUpdateAsync(recipeId, Arg.Any<CancellationToken>())
			.Returns<Recipe>(_ => throw new EntityNotFoundException(nameof(Recipe), recipeId.Value));

		var command = CreateValidCommand(recipeId);

		try { await handler.HandleAsync(command); } catch (EntityNotFoundException) { }

		await recipes.DidNotReceive().UpdateAsync(Arg.Any<Recipe>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_WrongOwner_DoesNotCallUpdateAsync()
	{
		var recipe = RecipeTestData.CreateRecipe("other-user");
		recipes.GetByIdForUpdateAsync(recipe.Id, Arg.Any<CancellationToken>()).Returns(recipe);

		var command = CreateValidCommand(recipe.Id);

		await handler.HandleAsync(command);

		await recipes.DidNotReceive().UpdateAsync(Arg.Any<Recipe>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_ValidCommand_CallsUpdateAsync()
	{
		var recipe = RecipeTestData.CreateRecipe();
		var recipeId = recipe.Id;
		recipes.GetByIdForUpdateAsync(recipeId, Arg.Any<CancellationToken>()).Returns(recipe);

		var command = CreateValidCommand(recipeId);

		await handler.HandleAsync(command);

		await recipes.Received(1).UpdateAsync(recipe, Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_FullUpdateCommand_UpdatesTitle()
	{
		var result = await ExecuteFullUpdateAsync();

		result.Value.Title.Should().Be("New Title");
	}

	[Fact]
	public async Task HandleAsync_FullUpdateCommand_UpdatesDescription()
	{
		var result = await ExecuteFullUpdateAsync();

		result.Value.Description.Should().Be("New description");
	}

	[Fact]
	public async Task HandleAsync_FullUpdateCommand_UpdatesServings()
	{
		var result = await ExecuteFullUpdateAsync();

		result.Value.Servings.Should().Be(6);
	}

	[Fact]
	public async Task HandleAsync_FullUpdateCommand_UpdatesPrepTimeMinutes()
	{
		var result = await ExecuteFullUpdateAsync();

		result.Value.PrepTimeMinutes.Should().Be(15);
	}

	[Fact]
	public async Task HandleAsync_FullUpdateCommand_UpdatesCookTimeMinutes()
	{
		var result = await ExecuteFullUpdateAsync();

		result.Value.CookTimeMinutes.Should().Be(30);
	}

	[Fact]
	public async Task HandleAsync_FullUpdateCommand_UpdatesDifficulty()
	{
		var result = await ExecuteFullUpdateAsync();

		result.Value.Difficulty.Should().Be("hard");
	}

	[Fact]
	public async Task HandleAsync_FullUpdateCommand_UpdatesImageUrl()
	{
		var result = await ExecuteFullUpdateAsync();

		result.Value.ImageUrl.Should().Be("https://example.com/new.jpg");
	}

	[Fact]
	public async Task HandleAsync_FullUpdateCommand_ReplacesIngredients()
	{
		var result = await ExecuteFullUpdateAsync();

		result.Value.Ingredients[0].Name.Should().Be("Butter");
	}

	[Fact]
	public async Task HandleAsync_FullUpdateCommand_ReplacesSteps()
	{
		var result = await ExecuteFullUpdateAsync();

		result.Value.Steps[0].Description.Should().Be("Melt butter");
	}

	[Fact]
	public async Task HandleAsync_ForwardsCancellationTokenToGetByIdForUpdate()
	{
		var recipe = RecipeTestData.CreateRecipe();
		var recipeId = recipe.Id;
		recipes.GetByIdForUpdateAsync(recipeId, Arg.Any<CancellationToken>()).Returns(recipe);
		var cts = new CancellationTokenSource();
		var command = CreateValidCommand(recipeId);

		await handler.HandleAsync(command, cts.Token);

		await recipes.Received(1).GetByIdForUpdateAsync(recipeId, cts.Token);
	}

	[Fact]
	public async Task HandleAsync_ForwardsCancellationTokenToUpdate()
	{
		var recipe = RecipeTestData.CreateRecipe();
		var recipeId = recipe.Id;
		recipes.GetByIdForUpdateAsync(recipeId, Arg.Any<CancellationToken>()).Returns(recipe);
		var cts = new CancellationTokenSource();
		var command = CreateValidCommand(recipeId);

		await handler.HandleAsync(command, cts.Token);

		await recipes.Received(1).UpdateAsync(recipe, cts.Token);
	}

	[Fact]
	public async Task HandleAsync_PreservesSourceUrl()
	{
		var recipe = RecipeTestData.CreateRecipeWithSourceUrl();
		var recipeId = recipe.Id;
		recipes.GetByIdForUpdateAsync(recipeId, Arg.Any<CancellationToken>()).Returns(recipe);

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
			[Ingredient.Create(IngredientName.From("Flour"), null)],
			[Step.Create(StepNumber.From(1), StepDescription.From("Mix"))],
			RecipeDescription.From("Old description"),
			Servings.From(4),
			TimingInfo.Of(PreparationTime.From(10), CookingTime.From(20)),
			Difficulty.From("easy"),
			ImageUrl.From("https://example.com/old.jpg"));

		var recipeId = recipe.Id;
		recipes.GetByIdForUpdateAsync(recipeId, Arg.Any<CancellationToken>()).Returns(recipe);

		var command = new UpdateRecipeCommand(
			recipeId,
			RecipeTitle.From("Updated"),
			[new SaveRecipeIngredientItem(IngredientName.From("Butter"), null)],
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
		var recipe = RecipeTestData.CreateRecipe();
		var recipeId = recipe.Id;
		var originalCreatedAt = recipe.CreatedAt;
		recipes.GetByIdForUpdateAsync(recipeId, Arg.Any<CancellationToken>()).Returns(recipe);

		var command = CreateValidCommand(recipeId);

		var result = await handler.HandleAsync(command);

		result.Value.CreatedAt.Should().Be(originalCreatedAt);
	}

	private static UpdateRecipeCommand CreateValidCommand(RecipeIdentifier identifier)
	{
		return new UpdateRecipeCommand(
			identifier,
			RecipeTitle.From("Updated Pasta"),
			[new SaveRecipeIngredientItem(IngredientName.From("Spaghetti"), Quantity.Of(Amount.From(400), Unit.Gram))],
			[new SaveRecipeStepItem(StepNumber.From(1), StepDescription.From("Cook pasta"))]);
	}

	private static UpdateRecipeCommand CreateFullUpdateCommand(RecipeIdentifier identifier)
	{
		return new UpdateRecipeCommand(
			identifier,
			RecipeTitle.From("New Title"),
			[new SaveRecipeIngredientItem(IngredientName.From("Butter"), Quantity.Of(Amount.From(200), Unit.Gram))],
			[new SaveRecipeStepItem(StepNumber.From(1), StepDescription.From("Melt butter"))],
			RecipeDescription.From("New description"),
			Servings.From(6),
			TimingInfo.Of(PreparationTime.From(15), CookingTime.From(30)),
			Difficulty.From("hard"),
			ImageUrl.From("https://example.com/new.jpg"));
	}

	private async Task<Result<RecipeDetailDto>> ExecuteFullUpdateAsync()
	{
		var recipe = RecipeTestData.CreateRecipe();
		recipes.GetByIdForUpdateAsync(recipe.Id, Arg.Any<CancellationToken>()).Returns(recipe);
		var command = CreateFullUpdateCommand(recipe.Id);

		return await handler.HandleAsync(command);
	}
}
