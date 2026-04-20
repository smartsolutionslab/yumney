using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using Xunit;
using static SmartSolutionsLab.Yumney.MealPlan.Application.Tests.MealPlanTestFixture;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Tests.Commands;

public class ClearMealSlotCommandHandlerTests
{
	private readonly IWeeklyPlanRepository plans = Substitute.For<IWeeklyPlanRepository>();
	private readonly IMealPlanUnitOfWork unitOfWork = Substitute.For<IMealPlanUnitOfWork>();
	private readonly ClearMealSlotCommandHandler handler;

	public ClearMealSlotCommandHandlerTests()
	{
		unitOfWork.Plans.Returns(plans);
		handler = new ClearMealSlotCommandHandler(unitOfWork, CreateCurrentUser());
	}

	[Fact]
	public async Task HandleAsync_ClearsSlotAndSaves()
	{
		CreatePlanWithRecipe(plans, DayOfWeek.Wednesday);

		var command = new ClearMealSlotCommand(TestWeek, DayOfWeek.Wednesday);

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();
		result.Value.Slots.First(s => s.Day == "Wednesday").IsEmpty.Should().BeTrue();
		await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
	}
}
