using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;
using static SmartSolutionsLab.Yumney.MealPlan.Application.Tests.MealPlanTestFixture;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Tests.Commands;

public class GenerateShoppingListCommandHandlerTests
{
	private readonly FakeMealPlanReadModelRepository readModel = new();
	private readonly IRecipeIngredientProvider ingredientProvider = Substitute.For<IRecipeIngredientProvider>();
	private readonly IStaplesProvider staplesProvider = Substitute.For<IStaplesProvider>();
	private readonly IShoppingListWriter shoppingListWriter = Substitute.For<IShoppingListWriter>();
	private readonly GenerateShoppingListCommandHandler handler;

	public GenerateShoppingListCommandHandlerTests()
	{
		staplesProvider.GetStapleNamesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns(new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "salt", "pepper", "flour" });
		handler = new GenerateShoppingListCommandHandler(readModel, ingredientProvider, staplesProvider, shoppingListWriter, CreateCurrentUser());
	}

	[Fact]
	public async Task HandleAsync_NoPlan_ReturnsNoRecipes()
	{
		var result = await handler.HandleAsync(new GenerateShoppingListCommand(TestWeek));

		result.IsSuccess.Should().BeFalse();
		result.Error!.Code.Should().Be("MEALPLAN_NO_RECIPES");
	}

	[Fact]
	public async Task HandleAsync_NoRecipeSlots_ReturnsFailure()
	{
		readModel.Seed(CreatePlan());

		var result = await handler.HandleAsync(new GenerateShoppingListCommand(TestWeek));

		result.IsSuccess.Should().BeFalse();
		result.Error!.Code.Should().Be("MEALPLAN_NO_RECIPES");
	}

	[Fact]
	public async Task HandleAsync_SingleRecipe_AddsIngredientsToShoppingList()
	{
		var recipeId = SeedPlanWithRecipeId("Pasta", 4);

		ingredientProvider.GetIngredientsAsync(recipeId, Arg.Any<CancellationToken>())
			.Returns(new List<RecipeIngredientInfo>
			{
				new("Spaghetti", 500m, "g", 4),
				new("Tomatoes", 400m, "g", 4),
			});

		var result = await handler.HandleAsync(new GenerateShoppingListCommand(TestWeek));

		result.IsSuccess.Should().BeTrue();
		result.Value.ItemsAdded.Should().Be(2);
		await shoppingListWriter.Received(1).AddItemsAsync(
			"user-123",
			Arg.Is<IReadOnlyList<ShoppingItemRequest>>(items => items.Count == 2),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_ScalesQuantitiesByServings()
	{
		var recipeId = SeedPlanWithRecipeId("Pasta", 8);

		ingredientProvider.GetIngredientsAsync(recipeId, Arg.Any<CancellationToken>())
			.Returns(new List<RecipeIngredientInfo>
			{
				new("Spaghetti", 500m, "g", 4),
			});

		var result = await handler.HandleAsync(new GenerateShoppingListCommand(TestWeek));

		result.IsSuccess.Should().BeTrue();
		await shoppingListWriter.Received(1).AddItemsAsync(
			"user-123",
			Arg.Is<IReadOnlyList<ShoppingItemRequest>>(items =>
				items.Count == 1 && items[0].Quantity == 1000m),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_MergesSameIngredientAcrossRecipes()
	{
		var (recipeA, recipeB) = SeedPlanWithTwoRecipes("Pasta", 4, "Risotto", 4);

		ingredientProvider.GetIngredientsAsync(recipeA, Arg.Any<CancellationToken>())
			.Returns(new List<RecipeIngredientInfo>
			{
				new("Olive Oil", 2m, "tbsp", 4),
			});
		ingredientProvider.GetIngredientsAsync(recipeB, Arg.Any<CancellationToken>())
			.Returns(new List<RecipeIngredientInfo>
			{
				new("olive oil", 3m, "tbsp", 4),
			});

		var result = await handler.HandleAsync(new GenerateShoppingListCommand(TestWeek));

		result.IsSuccess.Should().BeTrue();
		result.Value.ItemsAdded.Should().Be(1);
		await shoppingListWriter.Received(1).AddItemsAsync(
			"user-123",
			Arg.Is<IReadOnlyList<ShoppingItemRequest>>(items =>
				items.Count == 1 && items[0].Quantity == 5m),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_FilterStaples_SkipsStapleItems()
	{
		var recipeId = SeedPlanWithRecipeId("Steak", 2);

		ingredientProvider.GetIngredientsAsync(recipeId, Arg.Any<CancellationToken>())
			.Returns(new List<RecipeIngredientInfo>
			{
				new("Steak", 500m, "g", 2),
				new("Salt", 1m, "tsp", 2),
				new("Pepper", 0.5m, "tsp", 2),
			});

		var result = await handler.HandleAsync(new GenerateShoppingListCommand(TestWeek));

		result.IsSuccess.Should().BeTrue();
		result.Value.ItemsAdded.Should().Be(1);
		result.Value.StaplesSkipped.Should().Be(2);
	}

	[Fact]
	public async Task HandleAsync_NoItemsAfterFiltering_DoesNotCallWriter()
	{
		var recipeId = SeedPlanWithRecipeId("Salt Toast", 2);

		ingredientProvider.GetIngredientsAsync(recipeId, Arg.Any<CancellationToken>())
			.Returns(new List<RecipeIngredientInfo>
			{
				new("Salt", 1m, "tsp", 2),
				new("Flour", 100m, "g", 2),
			});

		var result = await handler.HandleAsync(new GenerateShoppingListCommand(TestWeek));

		result.IsSuccess.Should().BeTrue();
		result.Value.ItemsAdded.Should().Be(0);
		await shoppingListWriter.DidNotReceive().AddItemsAsync(
			Arg.Any<string>(),
			Arg.Any<IReadOnlyList<ShoppingItemRequest>>(),
			Arg.Any<CancellationToken>());
	}

	private Guid SeedPlanWithRecipeId(string title, int servings)
	{
		var recipeId = Guid.NewGuid();
		var plan = CreatePlan();
		plan.AssignRecipe(DayOfWeek.Monday, Recipe(recipeId, title), servings: SlotServings.From(servings));
		readModel.Seed(plan);
		return recipeId;
	}

	private (Guid RecipeA, Guid RecipeB) SeedPlanWithTwoRecipes(string titleA, int servingsA, string titleB, int servingsB)
	{
		var recipeA = Guid.NewGuid();
		var recipeB = Guid.NewGuid();
		var plan = CreatePlan();
		plan.AssignRecipe(DayOfWeek.Monday, Recipe(recipeA, titleA), servings: SlotServings.From(servingsA));
		plan.AssignRecipe(DayOfWeek.Tuesday, Recipe(recipeB, titleB), servings: SlotServings.From(servingsB));
		readModel.Seed(plan);
		return (recipeA, recipeB);
	}
}
