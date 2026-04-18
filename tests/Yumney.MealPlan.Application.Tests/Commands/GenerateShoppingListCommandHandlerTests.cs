using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Tests.Commands;

public class GenerateShoppingListCommandHandlerTests
{
	private readonly IWeeklyPlanRepository plans = Substitute.For<IWeeklyPlanRepository>();
	private readonly IRecipeIngredientProvider ingredientProvider = Substitute.For<IRecipeIngredientProvider>();
	private readonly IStaplesProvider staplesProvider = Substitute.For<IStaplesProvider>();
	private readonly IShoppingListWriter shoppingListWriter = Substitute.For<IShoppingListWriter>();
	private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
	private readonly GenerateShoppingListCommandHandler handler;

	public GenerateShoppingListCommandHandlerTests()
	{
		currentUser.UserId.Returns("user-123");
		staplesProvider.GetStapleNamesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns(new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "salt", "pepper", "flour" });
		handler = new GenerateShoppingListCommandHandler(plans, ingredientProvider, staplesProvider, shoppingListWriter, currentUser);
	}

	[Fact]
	public async Task HandleAsync_NoPlanFound_ReturnsFailure()
	{
		plans.FindByOwnerAndWeekAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<WeekIdentifier>(), Arg.Any<CancellationToken>())
			.Returns((WeeklyPlan?)null);

		var result = await handler.HandleAsync(new GenerateShoppingListCommand(WeekIdentifier.From(2026, 15)));

		result.IsSuccess.Should().BeFalse();
		result.Error!.Code.Should().Be("MEALPLAN_NOT_FOUND");
	}

	[Fact]
	public async Task HandleAsync_NoRecipeSlots_ReturnsFailure()
	{
		var plan = WeeklyPlan.Create(OwnerIdentifier.From("user-123"), WeekIdentifier.From(2026, 15));
		plans.FindByOwnerAndWeekAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<WeekIdentifier>(), Arg.Any<CancellationToken>())
			.Returns(plan);

		var result = await handler.HandleAsync(new GenerateShoppingListCommand(WeekIdentifier.From(2026, 15)));

		result.IsSuccess.Should().BeFalse();
		result.Error!.Code.Should().Be("MEALPLAN_NO_RECIPES");
	}

	[Fact]
	public async Task HandleAsync_SingleRecipe_AddsIngredientsToShoppingList()
	{
		var recipeId = Guid.NewGuid();
		var plan = CreatePlanWithRecipe(recipeId, "Pasta", 4);

		ingredientProvider.GetIngredientsAsync(recipeId, Arg.Any<CancellationToken>())
			.Returns(new List<RecipeIngredientInfo>
			{
				new("Spaghetti", 500m, "g", 4),
				new("Tomatoes", 400m, "g", 4),
			});

		var result = await handler.HandleAsync(new GenerateShoppingListCommand(WeekIdentifier.From(2026, 15)));

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
		var recipeId = Guid.NewGuid();
		var plan = CreatePlanWithRecipe(recipeId, "Pasta", 8);

		ingredientProvider.GetIngredientsAsync(recipeId, Arg.Any<CancellationToken>())
			.Returns(new List<RecipeIngredientInfo>
			{
				new("Spaghetti", 500m, "g", 4),
			});

		var result = await handler.HandleAsync(new GenerateShoppingListCommand(WeekIdentifier.From(2026, 15)));

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
		var recipeA = Guid.NewGuid();
		var recipeB = Guid.NewGuid();
		var plan = CreatePlanWithTwoRecipes(recipeA, "Pasta", 4, recipeB, "Risotto", 4);

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

		var result = await handler.HandleAsync(new GenerateShoppingListCommand(WeekIdentifier.From(2026, 15)));

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
		var recipeId = Guid.NewGuid();
		var plan = CreatePlanWithRecipe(recipeId, "Steak", 2);

		ingredientProvider.GetIngredientsAsync(recipeId, Arg.Any<CancellationToken>())
			.Returns(new List<RecipeIngredientInfo>
			{
				new("Steak", 500m, "g", 2),
				new("Salt", 1m, "tsp", 2),
				new("Pepper", 0.5m, "tsp", 2),
			});

		var result = await handler.HandleAsync(new GenerateShoppingListCommand(WeekIdentifier.From(2026, 15)));

		result.IsSuccess.Should().BeTrue();
		result.Value.ItemsAdded.Should().Be(1);
		result.Value.StaplesSkipped.Should().Be(2);
	}

	[Fact]
	public async Task HandleAsync_NoItemsAfterFiltering_DoesNotCallWriter()
	{
		var recipeId = Guid.NewGuid();
		var plan = CreatePlanWithRecipe(recipeId, "Salt Toast", 2);

		ingredientProvider.GetIngredientsAsync(recipeId, Arg.Any<CancellationToken>())
			.Returns(new List<RecipeIngredientInfo>
			{
				new("Salt", 1m, "tsp", 2),
				new("Flour", 100m, "g", 2),
			});

		var result = await handler.HandleAsync(new GenerateShoppingListCommand(WeekIdentifier.From(2026, 15)));

		result.IsSuccess.Should().BeTrue();
		result.Value.ItemsAdded.Should().Be(0);
		await shoppingListWriter.DidNotReceive().AddItemsAsync(
			Arg.Any<string>(),
			Arg.Any<IReadOnlyList<ShoppingItemRequest>>(),
			Arg.Any<CancellationToken>());
	}

	private WeeklyPlan CreatePlanWithRecipe(Guid recipeId, string title, int servings)
	{
		var owner = OwnerIdentifier.From("user-123");
		var week = WeekIdentifier.From(2026, 15);
		var plan = WeeklyPlan.Create(owner, week);
		plan.AssignRecipe(DayOfWeek.Monday, SlotRecipeReference.From(recipeId, title), servings: SlotServings.From(servings));

		plans.FindByOwnerAndWeekAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<WeekIdentifier>(), Arg.Any<CancellationToken>())
			.Returns(plan);

		return plan;
	}

	private WeeklyPlan CreatePlanWithTwoRecipes(Guid recipeA, string titleA, int servingsA, Guid recipeB, string titleB, int servingsB)
	{
		var owner = OwnerIdentifier.From("user-123");
		var week = WeekIdentifier.From(2026, 15);
		var plan = WeeklyPlan.Create(owner, week);
		plan.AssignRecipe(DayOfWeek.Monday, SlotRecipeReference.From(recipeA, titleA), servings: SlotServings.From(servingsA));
		plan.AssignRecipe(DayOfWeek.Tuesday, SlotRecipeReference.From(recipeB, titleB), servings: SlotServings.From(servingsB));

		plans.FindByOwnerAndWeekAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<WeekIdentifier>(), Arg.Any<CancellationToken>())
			.Returns(plan);

		return plan;
	}
}
