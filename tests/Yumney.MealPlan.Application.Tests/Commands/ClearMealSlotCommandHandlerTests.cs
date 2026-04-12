using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Tests.Commands;

public class ClearMealSlotCommandHandlerTests
{
    private readonly IWeeklyPlanRepository plans = Substitute.For<IWeeklyPlanRepository>();
    private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
    private readonly ClearMealSlotCommandHandler handler;

    public ClearMealSlotCommandHandlerTests()
    {
        currentUser.UserId.Returns("user-123");
        handler = new ClearMealSlotCommandHandler(plans, currentUser);
    }

    [Fact]
    public async Task HandleAsync_ClearsSlotAndSaves()
    {
        var plan = WeeklyPlan.Create(OwnerIdentifier.From("user-123"), WeekIdentifier.From(2026, 15));
        plan.AssignRecipe(DayOfWeek.Wednesday, Guid.NewGuid(), "Pasta");
        plans.GetByOwnerAndWeekAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<WeekIdentifier>(), Arg.Any<CancellationToken>())
            .Returns(plan);

        var command = new ClearMealSlotCommand(2026, 15, DayOfWeek.Wednesday);

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Slots.First(s => s.Day == "Wednesday").IsEmpty.Should().BeTrue();
        await plans.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
