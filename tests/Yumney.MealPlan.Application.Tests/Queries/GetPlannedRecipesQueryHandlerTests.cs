using FluentAssertions;
using SmartSolutionsLab.Yumney.MealPlan.Application.Queries;
using SmartSolutionsLab.Yumney.MealPlan.Application.Queries.Handlers;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using Xunit;
using static SmartSolutionsLab.Yumney.MealPlan.Application.Tests.MealPlanTestFixture;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Tests.Queries;

public class GetPlannedRecipesQueryHandlerTests
{
	private readonly FakeMealPlanReadModelRepository readModel = new();
	private readonly GetPlannedRecipesQueryHandler handler;

	public GetPlannedRecipesQueryHandlerTests()
	{
		handler = new GetPlannedRecipesQueryHandler(readModel, CreateCurrentUser());
	}

	[Fact]
	public async Task HandleAsync_NoPlan_ReturnsEmptyRecipes()
	{
		var result = await handler.HandleAsync(new GetPlannedRecipesQuery(TestWeek));

		result.IsSuccess.Should().BeTrue();
		result.Value.Recipes.Should().BeEmpty();
	}

	[Fact]
	public async Task HandleAsync_OnlyRecipeSlots_Returned()
	{
		var plan = CreatePlan();
		plan.AssignRecipe(DayOfWeek.Monday, Recipe());
		plan.SetFreetext(DayOfWeek.Tuesday, FreetextLabel.From("Eating out"));
		plan.SetLeftover(DayOfWeek.Wednesday, DayOfWeek.Monday, MealType.Dinner, SlotRecipeTitle.From("Pasta"));
		readModel.Seed(plan);

		var result = await handler.HandleAsync(new GetPlannedRecipesQuery(TestWeek));

		result.Value.Recipes.Should().HaveCount(1);
		result.Value.Recipes[0].RecipeTitle.Should().Be("Pasta");
	}

	[Fact]
	public async Task HandleAsync_MultipleRecipes_AllReturned()
	{
		var plan = CreatePlan();
		plan.AssignRecipe(DayOfWeek.Monday, Recipe(), servings: SlotServings.From(4));
		plan.AssignRecipe(DayOfWeek.Wednesday, Recipe("Steak"), servings: SlotServings.From(6));
		plan.AssignRecipe(DayOfWeek.Friday, Recipe("Fish"), servings: SlotServings.From(2));
		readModel.Seed(plan);

		var result = await handler.HandleAsync(new GetPlannedRecipesQuery(TestWeek));

		result.Value.Recipes.Should().HaveCount(3);
	}

	[Fact]
	public async Task HandleAsync_IncludesServingsPerSlot()
	{
		var plan = CreatePlan();
		plan.AssignRecipe(DayOfWeek.Monday, Recipe(), servings: SlotServings.From(8));
		readModel.Seed(plan);

		var result = await handler.HandleAsync(new GetPlannedRecipesQuery(TestWeek));

		result.Value.Recipes[0].Servings.Should().Be(8);
	}

	[Fact]
	public async Task HandleAsync_EmptySlots_Excluded()
	{
		var plan = CreatePlan();
		readModel.Seed(plan);

		var result = await handler.HandleAsync(new GetPlannedRecipesQuery(TestWeek));

		result.Value.Recipes.Should().BeEmpty();
	}
}
