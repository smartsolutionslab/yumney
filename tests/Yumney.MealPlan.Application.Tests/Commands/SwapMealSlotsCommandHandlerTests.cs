using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Tests.Commands;

public class SwapMealSlotsCommandHandlerTests
{
    private readonly IWeeklyPlanRepository plans = Substitute.For<IWeeklyPlanRepository>();
    private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
    private readonly SwapMealSlotsCommandHandler handler;

    public SwapMealSlotsCommandHandlerTests()
    {
        currentUser.UserId.Returns("user-123");
        handler = new SwapMealSlotsCommandHandler(plans, currentUser);
    }

    [Fact]
    public async Task HandleAsync_SwapsAndSaves()
    {
        var plan = WeeklyPlan.Create(OwnerIdentifier.From("user-123"), WeekIdentifier.From(2026, 15));
        plan.AssignRecipe(DayOfWeek.Monday, Guid.NewGuid(), "Pasta");
        plans.GetByOwnerAndWeekAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<WeekIdentifier>(), Arg.Any<CancellationToken>())
            .Returns(plan);

        var command = new SwapMealSlotsCommand(WeekIdentifier.From(2026, 15), DayOfWeek.Monday, DayOfWeek.Wednesday);

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Slots.First(s => s.Day == "Monday").IsEmpty.Should().BeTrue();
        result.Value.Slots.First(s => s.Day == "Wednesday").RecipeTitle.Should().Be("Pasta");
        await plans.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NoPlan_ThrowsEntityNotFoundException()
    {
        plans.GetByOwnerAndWeekAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<WeekIdentifier>(), Arg.Any<CancellationToken>())
            .Returns<WeeklyPlan>(_ => throw new EntityNotFoundException(nameof(WeeklyPlan), "2026-W15"));

        var command = new SwapMealSlotsCommand(WeekIdentifier.From(2026, 15), DayOfWeek.Monday, DayOfWeek.Tuesday);

        var act = () => handler.HandleAsync(command);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }
}
