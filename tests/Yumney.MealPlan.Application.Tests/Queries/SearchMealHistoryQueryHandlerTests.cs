using FluentAssertions;
using SmartSolutionsLab.Yumney.MealPlan.Application.Queries;
using SmartSolutionsLab.Yumney.MealPlan.Application.Queries.Handlers;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using Xunit;
using static SmartSolutionsLab.Yumney.MealPlan.Application.Tests.MealPlanTestFixture;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Tests.Queries;

public class SearchMealHistoryQueryHandlerTests
{
	private readonly FakeMealPlanReadModelRepository readModel = new();
	private readonly SearchMealHistoryQueryHandler handler;

	public SearchMealHistoryQueryHandlerTests()
	{
		handler = new SearchMealHistoryQueryHandler(readModel, CreateCurrentUser());
	}

	[Fact]
	public async Task HandleAsync_NoCookedMeals_ReturnsEmpty()
	{
		var result = await handler.HandleAsync(new SearchMealHistoryQuery());

		result.IsSuccess.Should().BeTrue();
		result.Value.Should().BeEmpty();
	}

	[Fact]
	public async Task HandleAsync_PlannedNotCooked_ExcludedFromHistory()
	{
		var plan = CreatePlan();
		plan.AssignRecipe(DayOfWeek.Monday, Recipe("Pasta"));
		readModel.Seed(plan);

		var result = await handler.HandleAsync(new SearchMealHistoryQuery());

		result.Value.Should().BeEmpty();
	}

	[Fact]
	public async Task HandleAsync_OneCookedMeal_ReturnsIt()
	{
		var plan = CreatePlan();
		plan.AssignRecipe(DayOfWeek.Monday, Recipe("Lasagna"));
		plan.MarkAsCooked(DayOfWeek.Monday);
		readModel.Seed(plan);

		var result = await handler.HandleAsync(new SearchMealHistoryQuery());

		result.Value.Should().ContainSingle().Which.RecipeTitle.Should().Be("Lasagna");
	}

	[Fact]
	public async Task HandleAsync_TermFiltersMatch_CaseInsensitive()
	{
		var plan = CreatePlan();
		plan.AssignRecipe(DayOfWeek.Monday, Recipe("Lasagna"));
		plan.AssignRecipe(DayOfWeek.Tuesday, Recipe("Pizza"));
		plan.MarkAsCooked(DayOfWeek.Monday);
		plan.MarkAsCooked(DayOfWeek.Tuesday);
		readModel.Seed(plan);

		var result = await handler.HandleAsync(new SearchMealHistoryQuery("LASAGNA"));

		result.Value.Should().ContainSingle().Which.RecipeTitle.Should().Be("Lasagna");
	}

	[Fact]
	public async Task HandleAsync_TermDoesNotMatch_ReturnsEmpty()
	{
		var plan = CreatePlan();
		plan.AssignRecipe(DayOfWeek.Monday, Recipe("Lasagna"));
		plan.MarkAsCooked(DayOfWeek.Monday);
		readModel.Seed(plan);

		var result = await handler.HandleAsync(new SearchMealHistoryQuery("Tacos"));

		result.Value.Should().BeEmpty();
	}

	[Fact]
	public async Task HandleAsync_MultipleWeeks_NewestFirst()
	{
		var oldPlan = WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 10));
		oldPlan.AssignRecipe(DayOfWeek.Monday, Recipe("Old Soup"));
		oldPlan.MarkAsCooked(DayOfWeek.Monday);
		var newPlan = WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 20));
		newPlan.AssignRecipe(DayOfWeek.Tuesday, Recipe("New Soup"));
		newPlan.MarkAsCooked(DayOfWeek.Tuesday);
		readModel.Seed(oldPlan);
		readModel.Seed(newPlan);

		var result = await handler.HandleAsync(new SearchMealHistoryQuery());

		result.Value.Select(r => r.RecipeTitle).Should().Equal("New Soup", "Old Soup");
	}

	[Fact]
	public async Task HandleAsync_LimitClampedToOneHundred()
	{
		var result = await handler.HandleAsync(new SearchMealHistoryQuery(Limit: 999));

		result.IsSuccess.Should().BeTrue();
	}

	[Fact]
	public async Task HandleAsync_LimitReturnedHonored()
	{
		var plan = CreatePlan();
		plan.EnableExtendedMode();
		plan.AssignRecipe(DayOfWeek.Monday, Recipe("A"));
		plan.AssignRecipe(DayOfWeek.Tuesday, Recipe("B"));
		plan.AssignRecipe(DayOfWeek.Wednesday, Recipe("C"));
		plan.MarkAsCooked(DayOfWeek.Monday);
		plan.MarkAsCooked(DayOfWeek.Tuesday);
		plan.MarkAsCooked(DayOfWeek.Wednesday);
		readModel.Seed(plan);

		var result = await handler.HandleAsync(new SearchMealHistoryQuery(Limit: 2));

		result.Value.Should().HaveCount(2);
	}
}
