using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Tests.Commands;

public class ConfirmMealCommandHandlerTests
{
    private readonly IWeeklyPlanRepository plans = Substitute.For<IWeeklyPlanRepository>();
    private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
    private readonly ConfirmMealCommandHandler handler;

    public ConfirmMealCommandHandlerTests()
    {
        currentUser.UserId.Returns("user-123");
        handler = new ConfirmMealCommandHandler(plans, currentUser);
    }

    [Fact]
    public async Task HandleAsync_MarkAsCooked_SetsSlotStateToCooked()
    {
        var plan = CreatePlanWithRecipe();
        var command = new ConfirmMealCommand(2026, 15, DayOfWeek.Monday, MealType.Dinner, MealState.Cooked);

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        var slot = result.Value.Slots.First(s => s.Day == "Monday");
        slot.State.Should().Be("Cooked");
        await plans.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_MarkAsSkipped_SetsSlotStateToSkipped()
    {
        var plan = CreatePlanWithRecipe();
        var command = new ConfirmMealCommand(2026, 15, DayOfWeek.Monday, MealType.Dinner, MealState.Skipped);

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        var slot = result.Value.Slots.First(s => s.Day == "Monday");
        slot.State.Should().Be("Skipped");
    }

    [Fact]
    public async Task HandleAsync_ResetToPlanned_SetsSlotStateBackToPlanned()
    {
        var plan = CreatePlanWithRecipe();
        plan.MarkAsCooked(DayOfWeek.Monday, MealType.Dinner);

        var command = new ConfirmMealCommand(2026, 15, DayOfWeek.Monday, MealType.Dinner, MealState.Planned);

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        var slot = result.Value.Slots.First(s => s.Day == "Monday");
        slot.State.Should().Be("Planned");
    }

    private WeeklyPlan CreatePlanWithRecipe()
    {
        var owner = OwnerIdentifier.From("user-123");
        var week = WeekIdentifier.From(2026, 15);
        var plan = WeeklyPlan.Create(owner, week);
        plan.AssignRecipe(DayOfWeek.Monday, Guid.NewGuid(), "Pasta");

        plans.GetByOwnerAndWeekAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<WeekIdentifier>(), Arg.Any<CancellationToken>())
            .Returns(plan);

        return plan;
    }
}
