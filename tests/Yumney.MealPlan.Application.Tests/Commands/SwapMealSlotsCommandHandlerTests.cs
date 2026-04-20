using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;
using static SmartSolutionsLab.Yumney.MealPlan.Application.Tests.MealPlanTestFixture;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Tests.Commands;

public class SwapMealSlotsCommandHandlerTests
{
	private readonly IWeeklyPlanRepository plans = Substitute.For<IWeeklyPlanRepository>();
	private readonly SwapMealSlotsCommandHandler handler;

	public SwapMealSlotsCommandHandlerTests()
	{
		handler = new SwapMealSlotsCommandHandler(plans, CreateCurrentUser());
	}

	[Fact]
	public async Task HandleAsync_SwapsAndSaves()
	{
		CreatePlanWithRecipe(plans);

		var command = new SwapMealSlotsCommand(TestWeek, DayOfWeek.Monday, DayOfWeek.Wednesday);

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

		var command = new SwapMealSlotsCommand(TestWeek, DayOfWeek.Monday, DayOfWeek.Tuesday);

		var act = () => handler.HandleAsync(command);

		await act.Should().ThrowAsync<EntityNotFoundException>();
	}
}
