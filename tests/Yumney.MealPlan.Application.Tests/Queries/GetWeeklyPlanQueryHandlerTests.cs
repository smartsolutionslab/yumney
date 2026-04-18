using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.MealPlan.Application.Queries;
using SmartSolutionsLab.Yumney.MealPlan.Application.Queries.Handlers;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Tests.Queries;

public class GetWeeklyPlanQueryHandlerTests
{
    private readonly IWeeklyPlanRepository plans = Substitute.For<IWeeklyPlanRepository>();
    private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
    private readonly GetWeeklyPlanQueryHandler handler;

    public GetWeeklyPlanQueryHandlerTests()
    {
        currentUser.UserId.Returns("user-123");
        handler = new GetWeeklyPlanQueryHandler(plans, currentUser);
    }

    [Fact]
    public async Task HandleAsync_NoPlan_ReturnsEmpty7Slots()
    {
        plans.FindByOwnerAndWeekAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<WeekIdentifier>(), Arg.Any<CancellationToken>())
            .Returns((WeeklyPlan?)null);

        var result = await handler.HandleAsync(new GetWeeklyPlanQuery(WeekIdentifier.From(2026, 15)));

        result.IsSuccess.Should().BeTrue();
        result.Value.Week.Should().Be("2026-W15");
        result.Value.Slots.Should().HaveCount(7);
        result.Value.Slots.Should().OnlyContain(s => s.IsEmpty);
    }

    [Fact]
    public async Task HandleAsync_ExistingPlan_ReturnsSlotsWithRecipes()
    {
        var owner = OwnerIdentifier.From("user-123");
        var week = WeekIdentifier.From(2026, 15);
        var plan = WeeklyPlan.Create(owner, week);
        plan.AssignRecipe(DayOfWeek.Monday, Guid.NewGuid(), "Pasta");

        plans.FindByOwnerAndWeekAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<WeekIdentifier>(), Arg.Any<CancellationToken>())
            .Returns(plan);

        var result = await handler.HandleAsync(new GetWeeklyPlanQuery(WeekIdentifier.From(2026, 15)));

        result.IsSuccess.Should().BeTrue();
        result.Value.Slots.Should().Contain(s => s.RecipeTitle == "Pasta");
    }
}
