using FluentAssertions;
using SmartSolutionsLab.Yumney.MealPlan.Application.Queries;
using SmartSolutionsLab.Yumney.MealPlan.Application.Queries.Handlers;
using Xunit;
using static SmartSolutionsLab.Yumney.MealPlan.Application.Tests.MealPlanTestFixture;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Tests.Queries;

public class GetWeeklyPlanQueryHandlerTests
{
	private readonly FakeMealPlanReadModelRepository readModel = new();
	private readonly GetWeeklyPlanQueryHandler handler;

	public GetWeeklyPlanQueryHandlerTests()
	{
		handler = new GetWeeklyPlanQueryHandler(readModel, CreateCurrentUser());
	}

	[Fact]
	public async Task HandleAsync_NoPlan_ReturnsEmpty7Slots()
	{
		var result = await handler.HandleAsync(new GetWeeklyPlanQuery(TestWeek));

		result.IsSuccess.Should().BeTrue();
		result.Value.Week.Should().Be("2026-W15");
		result.Value.Slots.Should().HaveCount(7);
		result.Value.Slots.Should().OnlyContain(s => s.IsEmpty);
	}

	[Fact]
	public async Task HandleAsync_ExistingPlan_ReturnsSlotsWithRecipes()
	{
		var plan = CreatePlan();
		plan.AssignRecipe(DayOfWeek.Monday, Recipe());
		readModel.Seed(plan);

		var result = await handler.HandleAsync(new GetWeeklyPlanQuery(TestWeek));

		result.IsSuccess.Should().BeTrue();
		result.Value.Slots.Should().Contain(s => s.RecipeTitle == "Pasta");
	}
}
