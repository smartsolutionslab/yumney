using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Tests.Commands;

public class AssignRecipeCommandHandlerTests
{
    private readonly IWeeklyPlanRepository plans = Substitute.For<IWeeklyPlanRepository>();
    private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
    private readonly AssignRecipeCommandHandler handler;

    public AssignRecipeCommandHandlerTests()
    {
        currentUser.UserId.Returns("user-123");
        handler = new AssignRecipeCommandHandler(plans, currentUser);
    }

    [Fact]
    public async Task HandleAsync_NoPlanExists_CreatesNewPlanAndAssigns()
    {
        plans.FindByOwnerAndWeekAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<WeekIdentifier>(), Arg.Any<CancellationToken>())
            .Returns((WeeklyPlan?)null);

        var command = new AssignRecipeCommand(WeekIdentifier.From(2026, 15), DayOfWeek.Monday, SlotRecipeReference.From(Guid.NewGuid(), "Pasta"));

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Slots.Should().HaveCount(7);
        await plans.Received(1).AddAsync(Arg.Any<WeeklyPlan>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_PlanExists_UpdatesExisting()
    {
        var owner = OwnerIdentifier.From("user-123");
        var week = WeekIdentifier.From(2026, 15);
        var existing = WeeklyPlan.Create(owner, week);

        plans.FindByOwnerAndWeekAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<WeekIdentifier>(), Arg.Any<CancellationToken>())
            .Returns(existing);
        plans.GetByOwnerAndWeekAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<WeekIdentifier>(), Arg.Any<CancellationToken>())
            .Returns(existing);

        var command = new AssignRecipeCommand(WeekIdentifier.From(2026, 15), DayOfWeek.Wednesday, SlotRecipeReference.From(Guid.NewGuid(), "Steak"), Servings: SlotServings.From(6));

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        await plans.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await plans.DidNotReceive().AddAsync(Arg.Any<WeeklyPlan>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ReturnsCorrectWeekInDto()
    {
        plans.FindByOwnerAndWeekAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<WeekIdentifier>(), Arg.Any<CancellationToken>())
            .Returns((WeeklyPlan?)null);

        var command = new AssignRecipeCommand(WeekIdentifier.From(2026, 15), DayOfWeek.Friday, SlotRecipeReference.From(Guid.NewGuid(), "Fish"));

        var result = await handler.HandleAsync(command);

        result.Value.Week.Should().Be("2026-W15");
    }

    [Fact]
    public async Task HandleAsync_WithBreakfastMealType_AssignsToBreakfastSlot()
    {
        var owner = OwnerIdentifier.From("user-123");
        var week = WeekIdentifier.From(2026, 15);
        var existing = WeeklyPlan.Create(owner, week);
        existing.EnableExtendedMode();

        plans.FindByOwnerAndWeekAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<WeekIdentifier>(), Arg.Any<CancellationToken>())
            .Returns(existing);
        plans.GetByOwnerAndWeekAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<WeekIdentifier>(), Arg.Any<CancellationToken>())
            .Returns(existing);

        var command = new AssignRecipeCommand(WeekIdentifier.From(2026, 15), DayOfWeek.Monday, SlotRecipeReference.From(Guid.NewGuid(), "Pancakes"), MealType.Breakfast);

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Slots.Should().Contain(s => s.RecipeTitle == "Pancakes" && s.MealType == "Breakfast");
    }
}
