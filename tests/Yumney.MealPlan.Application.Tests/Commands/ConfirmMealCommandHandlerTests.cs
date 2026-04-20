using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using Xunit;
using static SmartSolutionsLab.Yumney.MealPlan.Application.Tests.MealPlanTestFixture;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Tests.Commands;

public class ConfirmMealCommandHandlerTests
{
	private readonly IWeeklyPlanRepository plans = Substitute.For<IWeeklyPlanRepository>();
	private readonly ConfirmMealCommandHandler handler;

	public ConfirmMealCommandHandlerTests()
	{
		handler = new ConfirmMealCommandHandler(plans, CreateCurrentUser());
	}

	[Fact]
	public async Task HandleAsync_MarkAsCooked_SetsSlotStateToCooked()
	{
		CreatePlanWithRecipe(plans);
		var command = new ConfirmMealCommand(TestWeek, DayOfWeek.Monday, MealType.Dinner, MealState.Cooked);

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();
		var slot = result.Value.Slots.First(s => s.Day == "Monday");
		slot.State.Should().Be("Cooked");
		await plans.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_MarkAsSkipped_SetsSlotStateToSkipped()
	{
		CreatePlanWithRecipe(plans);
		var command = new ConfirmMealCommand(TestWeek, DayOfWeek.Monday, MealType.Dinner, MealState.Skipped);

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();
		var slot = result.Value.Slots.First(s => s.Day == "Monday");
		slot.State.Should().Be("Skipped");
	}

	[Fact]
	public async Task HandleAsync_ResetToPlanned_SetsSlotStateBackToPlanned()
	{
		var plan = CreatePlanWithRecipe(plans);
		plan.MarkAsCooked(DayOfWeek.Monday, MealType.Dinner);

		var command = new ConfirmMealCommand(TestWeek, DayOfWeek.Monday, MealType.Dinner, MealState.Planned);

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();
		var slot = result.Value.Slots.First(s => s.Day == "Monday");
		slot.State.Should().Be("Planned");
	}
}
