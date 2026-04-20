using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.MealPlan.Application.Queries;
using SmartSolutionsLab.Yumney.MealPlan.Application.Queries.Handlers;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;
using static SmartSolutionsLab.Yumney.MealPlan.Application.Tests.MealPlanTestFixture;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Tests.Queries;

public class GetWeeklyPlanQueryHandlerTests
{
	private readonly IWeeklyPlanRepository plans = Substitute.For<IWeeklyPlanRepository>();
	private readonly GetWeeklyPlanQueryHandler handler;

	public GetWeeklyPlanQueryHandlerTests()
	{
		handler = new GetWeeklyPlanQueryHandler(plans, CreateCurrentUser());
	}

	[Fact]
	public async Task HandleAsync_NoPlan_ReturnsEmpty7Slots()
	{
		plans.FindByOwnerAndWeekAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<WeekIdentifier>(), Arg.Any<CancellationToken>())
			.Returns((WeeklyPlan?)null);

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

		plans.FindByOwnerAndWeekAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<WeekIdentifier>(), Arg.Any<CancellationToken>())
			.Returns(plan);

		var result = await handler.HandleAsync(new GetWeeklyPlanQuery(TestWeek));

		result.IsSuccess.Should().BeTrue();
		result.Value.Slots.Should().Contain(s => s.RecipeTitle == "Pasta");
	}
}
