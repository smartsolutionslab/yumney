using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Tests.Commands;

public class SaveRecipeCommandHandlerTests
{
	private readonly IRecipeRepository recipes = Substitute.For<IRecipeRepository>();
	private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
	private readonly SaveRecipeCommandHandler handler;

	public SaveRecipeCommandHandlerTests()
	{
		currentUser.UserId.Returns("user-123");
		handler = new SaveRecipeCommandHandler(recipes, currentUser);
	}

	[Fact]
	public async Task HandleAsync_ValidCommand_ReturnsSuccess()
	{
		var command = CreateValidCommand();

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();
	}

	[Fact]
	public async Task HandleAsync_ValidCommand_ReturnsDtoWithMatchingTitle()
	{
		var command = CreateValidCommand();

		var result = await handler.HandleAsync(command);

		result.Value.Title.Should().Be("Pasta Carbonara");
	}

	[Fact]
	public async Task HandleAsync_ValidCommand_ReturnsDtoWithRecentCreatedAt()
	{
		var command = CreateValidCommand();

		var result = await handler.HandleAsync(command);

		result.Value.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
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

		var flour = IngredientName.From("Flour");
		var sugar = IngredientName.From("Sugar");
		var command = new SaveRecipeCommand(
			RecipeTitle.From("Test"),
			[
				new SaveRecipeIngredientItem(flour, Quantity.Of(Amount.From(500), Unit.Gram)),
				new SaveRecipeIngredientItem(sugar, Quantity.Of(Amount.From(100), null)),
			],
			[new SaveRecipeStepItem(StepNumber.From(1), StepDescription.From("Mix"))],
			SourceUrl: RecipeUrl.From("https://example.com/recipe"));

		await handler.HandleAsync(command);

		capturedRecipe.Should().NotBeNull();
		capturedRecipe!.Ingredients.Should().HaveCount(2);
		capturedRecipe.Ingredients[0].Name.Should().Be(flour);
		capturedRecipe.Ingredients[1].Name.Should().Be(sugar);
		capturedRecipe.Ingredients[1].Quantity!.Unit.Should().BeNull();
	}

	[Fact]
	public async Task HandleAsync_ValidCommand_CreatesRecipeWithCorrectSteps()
	{
		Recipe? capturedRecipe = null;
		await recipes.AddAsync(Arg.Do<Recipe>(r => capturedRecipe = r), Arg.Any<CancellationToken>());

		var preheatOven = StepDescription.From("Preheat oven");
		var mixIngredients = StepDescription.From("Mix ingredients");
		var command = new SaveRecipeCommand(
			RecipeTitle.From("Test"),
			[new SaveRecipeIngredientItem(IngredientName.From("Flour"), null)],
			[
				new SaveRecipeStepItem(StepNumber.From(1), preheatOven),
				new SaveRecipeStepItem(StepNumber.From(2), mixIngredients),
			],
			SourceUrl: RecipeUrl.From("https://example.com/recipe"));

		await handler.HandleAsync(command);

		capturedRecipe.Should().NotBeNull();
		capturedRecipe!.Steps.Should().HaveCount(2);
		capturedRecipe.Steps[0].Description.Should().Be(preheatOven);
		capturedRecipe.Steps[1].Description.Should().Be(mixIngredients);
	}

	[Fact]
	public async Task HandleAsync_WithDescription_PassesDescriptionThrough()
	{
		var description = RecipeDescription.From("A test recipe");
		var command = CreateCommandWithOptionalField(description: description);

		var capturedRecipe = await CaptureSavedRecipeAsync(command);

		capturedRecipe.Description.Should().Be(description);
	}

	[Fact]
	public async Task HandleAsync_WithServings_PassesServingsThrough()
	{
		var servings = Servings.From(4);
		var command = CreateCommandWithOptionalField(servings: servings);

		var capturedRecipe = await CaptureSavedRecipeAsync(command);

		capturedRecipe.Servings.Should().Be(servings);
	}

	[Fact]
	public async Task HandleAsync_WithPreparationTime_PassesPreparationTimeThrough()
	{
		var preparationTime = PreparationTime.From(10);
		var command = CreateCommandWithOptionalField(
			timing: TimingInfo.Of(preparationTime, CookingTime.From(20)));

		var capturedRecipe = await CaptureSavedRecipeAsync(command);

		capturedRecipe.Timing?.Preparation.Should().Be(preparationTime);
	}

	[Fact]
	public async Task HandleAsync_WithCookingTime_PassesCookingTimeThrough()
	{
		var cookingTime = CookingTime.From(20);
		var command = CreateCommandWithOptionalField(
			timing: TimingInfo.Of(PreparationTime.From(10), cookingTime));

		var capturedRecipe = await CaptureSavedRecipeAsync(command);

		capturedRecipe.Timing?.Cooking.Should().Be(cookingTime);
	}

	[Fact]
	public async Task HandleAsync_WithDifficulty_PassesDifficultyThrough()
	{
		var difficulty = Difficulty.From("easy");
		var command = CreateCommandWithOptionalField(difficulty: difficulty);

		var capturedRecipe = await CaptureSavedRecipeAsync(command);

		capturedRecipe.Difficulty.Should().Be(difficulty);
	}

	[Fact]
	public async Task HandleAsync_WithImageUrl_PassesImageUrlThrough()
	{
		var imageUrl = ImageUrl.From("https://example.com/image.jpg");
		var command = CreateCommandWithOptionalField(imageUrl: imageUrl);

		var capturedRecipe = await CaptureSavedRecipeAsync(command);

		capturedRecipe.ImageUrl.Should().Be(imageUrl);
	}

	[Fact]
	public async Task HandleAsync_ValidCommand_ReturnsDtoWithMatchingIdentifier()
	{
		Recipe? capturedRecipe = null;
		await recipes.AddAsync(Arg.Do<Recipe>(r => capturedRecipe = r), Arg.Any<CancellationToken>());

		var command = CreateValidCommand();

		var result = await handler.HandleAsync(command);

		capturedRecipe.Should().NotBeNull();
		result.Value.Identifier.Should().Be(capturedRecipe!.Id.Value);
	}

	[Fact]
	public async Task HandleAsync_ForwardsCancellationTokenToExistsCheck()
	{
		var cts = new CancellationTokenSource();
		var command = CreateValidCommand();

		await handler.HandleAsync(command, cts.Token);

		await recipes.Received(1).ExistsBySourceUrlAsync(
			Arg.Any<RecipeUrl>(),
			Arg.Any<OwnerIdentifier>(),
			cts.Token);
	}

	[Fact]
	public async Task HandleAsync_ForwardsCancellationTokenToAdd()
	{
		var cts = new CancellationTokenSource();
		var command = CreateValidCommand();

		await handler.HandleAsync(command, cts.Token);

		await recipes.Received(1).AddAsync(Arg.Any<Recipe>(), cts.Token);
	}

	[Fact]
	public async Task HandleAsync_NullSourceUrl_SkipsDuplicateCheck()
	{
		var command = new SaveRecipeCommand(
			RecipeTitle.From("Manual Recipe"),
			[new SaveRecipeIngredientItem(IngredientName.From("Flour"), null)],
			[new SaveRecipeStepItem(StepNumber.From(1), StepDescription.From("Mix"))]);

		await handler.HandleAsync(command);

		await recipes.DidNotReceive().ExistsBySourceUrlAsync(
			Arg.Any<RecipeUrl>(),
			Arg.Any<OwnerIdentifier>(),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_NullSourceUrl_ReturnsSuccess()
	{
		var command = CreateManualCommand();

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();
	}

	[Fact]
	public async Task HandleAsync_NullSourceUrl_CallsAddAsync()
	{
		var command = CreateManualCommand();

		await handler.HandleAsync(command);

		await recipes.Received(1).AddAsync(Arg.Any<Recipe>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_NullSourceUrl_CreatesRecipeWithNullSourceUrl()
	{
		var capturedRecipe = await CaptureSavedRecipeAsync(CreateManualCommand());

		capturedRecipe.SourceUrl.Should().BeNull();
	}

	private static SaveRecipeCommand CreateValidCommand()
	{
		return new SaveRecipeCommand(
			RecipeTitle.From("Pasta Carbonara"),
			[new SaveRecipeIngredientItem(IngredientName.From("Spaghetti"), Quantity.Of(Amount.From(400), Unit.Gram))],
			[new SaveRecipeStepItem(StepNumber.From(1), StepDescription.From("Cook pasta"))],
			SourceUrl: RecipeUrl.From("https://example.com/recipe"));
	}

	private static SaveRecipeCommand CreateCommandWithOptionalField(
		RecipeDescription? description = null,
		Servings? servings = null,
		TimingInfo? timing = null,
		Difficulty? difficulty = null,
		ImageUrl? imageUrl = null)
	{
		return new SaveRecipeCommand(
			RecipeTitle.From("Test"),
			[new SaveRecipeIngredientItem(IngredientName.From("Flour"), null)],
			[new SaveRecipeStepItem(StepNumber.From(1), StepDescription.From("Mix"))],
			description,
			servings,
			timing,
			difficulty,
			imageUrl,
			SourceUrl: RecipeUrl.From("https://example.com/recipe"));
	}

	private static SaveRecipeCommand CreateManualCommand()
	{
		return new SaveRecipeCommand(
			RecipeTitle.From("Manual Recipe"),
			[new SaveRecipeIngredientItem(IngredientName.From("Flour"), null)],
			[new SaveRecipeStepItem(StepNumber.From(1), StepDescription.From("Mix"))]);
	}

	private async Task<Recipe> CaptureSavedRecipeAsync(SaveRecipeCommand command)
	{
		Recipe? capturedRecipe = null;
		await recipes.AddAsync(Arg.Do<Recipe>(r => capturedRecipe = r), Arg.Any<CancellationToken>());

		await handler.HandleAsync(command);

		capturedRecipe.Should().NotBeNull();
		return capturedRecipe!;
	}
}
