using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shopping.Application.Commands;
using SmartSolutionsLab.Yumney.Shopping.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Tests.Commands;

public class CreateShoppingListFromRecipesCommandHandlerTests
{
	private readonly IRecipeIngredientLookup recipeLookup = Substitute.For<IRecipeIngredientLookup>();
	private readonly IShoppingListEventStore eventStore = Substitute.For<IShoppingListEventStore>();
	private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
	private readonly CreateShoppingListFromRecipesCommandHandler handler;

	public CreateShoppingListFromRecipesCommandHandlerTests()
	{
		currentUser.UserId.Returns("user-123");
		handler = new CreateShoppingListFromRecipesCommandHandler(recipeLookup, eventStore, currentUser);
	}

	[Fact]
	public async Task HandleAsync_NoRecipesProvided_ReturnsFailure()
	{
		var command = new CreateShoppingListFromRecipesCommand(
			ShoppingListTitle.From("Empty"),
			[]);

		var result = await handler.HandleAsync(command);

		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(CreateShoppingListFromRecipesErrors.NoRecipesProvided);
	}

	[Fact]
	public async Task HandleAsync_AllRecipesYieldNoIngredients_ReturnsFailure()
	{
		var recipe = RecipeReference.New();
		recipeLookup.LookupAsync(recipe, Arg.Any<CancellationToken>()).Returns([]);

		var command = new CreateShoppingListFromRecipesCommand(
			ShoppingListTitle.From("Empty"),
			[new RecipeSelection(recipe, Servings.From(4))]);

		var result = await handler.HandleAsync(command);

		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(CreateShoppingListFromRecipesErrors.NoIngredientsResolved);
	}

	[Fact]
	public async Task HandleAsync_TwoRecipesWithSharedIngredient_MergesAndPersists()
	{
		var pasta = RecipeReference.New();
		var risotto = RecipeReference.New();
		recipeLookup.LookupAsync(pasta, Arg.Any<CancellationToken>()).Returns(new[]
		{
			new RecipeIngredientLookupResult("Onion", 1m, null, 4),
			new RecipeIngredientLookupResult("Olive Oil", 2m, "tbsp", 4),
		});
		recipeLookup.LookupAsync(risotto, Arg.Any<CancellationToken>()).Returns(new[]
		{
			new RecipeIngredientLookupResult("Onion", 2m, null, 4),
			new RecipeIngredientLookupResult("Rice", 300m, "g", 4),
		});

		var command = new CreateShoppingListFromRecipesCommand(
			ShoppingListTitle.From("Meal Prep"),
			[
				new RecipeSelection(pasta, Servings.From(4)),
				new RecipeSelection(risotto, Servings.From(4)),
			]);

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();
		result.Value.Items.Should().HaveCount(3);
		result.Value.Items.Should().ContainSingle(item => item.Name == "Onion" && item.Amount == 3);
		result.Value.Items.Should().ContainSingle(item => item.Name == "Olive Oil" && item.Amount == 2 && item.Unit == "tbsp");
		result.Value.Items.Should().ContainSingle(item => item.Name == "Rice" && item.Amount == 300 && item.Unit == "g");
	}

	[Fact]
	public async Task HandleAsync_ScalesPerRecipeBeforeMerging()
	{
		var pasta = RecipeReference.New();
		var risotto = RecipeReference.New();
		recipeLookup.LookupAsync(pasta, Arg.Any<CancellationToken>()).Returns(new[]
		{
			new RecipeIngredientLookupResult("Onion", 1m, null, 4),
		});
		recipeLookup.LookupAsync(risotto, Arg.Any<CancellationToken>()).Returns(new[]
		{
			new RecipeIngredientLookupResult("Onion", 2m, null, 4),
		});

		var command = new CreateShoppingListFromRecipesCommand(
			ShoppingListTitle.From("Meal Prep"),
			[
				new RecipeSelection(pasta, Servings.From(8)),
				new RecipeSelection(risotto, Servings.From(2)),
			]);

		var result = await handler.HandleAsync(command);

		result.Value.Items.Should().ContainSingle(item => item.Name == "Onion" && item.Amount == 3);
	}

	[Fact]
	public async Task HandleAsync_PersistsToEventStore()
	{
		var recipe = RecipeReference.New();
		recipeLookup.LookupAsync(recipe, Arg.Any<CancellationToken>()).Returns(new[]
		{
			new RecipeIngredientLookupResult("Flour", 200m, "g", 4),
		});

		var command = new CreateShoppingListFromRecipesCommand(
			ShoppingListTitle.From("Meal Prep"),
			[new RecipeSelection(recipe, Servings.From(4))]);

		await handler.HandleAsync(command);

		await eventStore.Received(1).SaveAsync(Arg.Any<ShoppingList>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_DoesNotAttachRecipeReference()
	{
		var recipe = RecipeReference.New();
		recipeLookup.LookupAsync(recipe, Arg.Any<CancellationToken>()).Returns(new[]
		{
			new RecipeIngredientLookupResult("Flour", 200m, "g", 4),
		});

		var command = new CreateShoppingListFromRecipesCommand(
			ShoppingListTitle.From("Meal Prep"),
			[new RecipeSelection(recipe, Servings.From(4))]);

		var result = await handler.HandleAsync(command);

		result.Value.RecipeReference.Should().BeNull();
	}

	[Fact]
	public async Task HandleAsync_ForwardsCancellationTokenToLookupAndStore()
	{
		var recipe = RecipeReference.New();
		recipeLookup.LookupAsync(recipe, Arg.Any<CancellationToken>()).Returns(new[]
		{
			new RecipeIngredientLookupResult("Flour", 200m, "g", 4),
		});

		using var cts = new CancellationTokenSource();
		var command = new CreateShoppingListFromRecipesCommand(
			ShoppingListTitle.From("Meal Prep"),
			[new RecipeSelection(recipe, Servings.From(4))]);

		await handler.HandleAsync(command, cts.Token);

		await recipeLookup.Received(1).LookupAsync(recipe, cts.Token);
		await eventStore.Received(1).SaveAsync(Arg.Any<ShoppingList>(), cts.Token);
	}
}
