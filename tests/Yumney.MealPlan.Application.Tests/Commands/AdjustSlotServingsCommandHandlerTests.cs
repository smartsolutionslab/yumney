using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;
using static SmartSolutionsLab.Yumney.MealPlan.Application.Tests.MealPlanTestFixture;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Tests.Commands;

public class AdjustSlotServingsCommandHandlerTests
{
	private readonly IWeeklyPlanRepository plans = Substitute.For<IWeeklyPlanRepository>();
	private readonly AdjustSlotServingsCommandHandler handler;

	public AdjustSlotServingsCommandHandlerTests()
	{
		handler = new AdjustSlotServingsCommandHandler(plans, CreateCurrentUser());
	}

	[Fact]
	public async Task HandleAsync_AdjustsServingsAndSaves()
	{
		CreatePlanWithRecipe(plans);

		var command = new AdjustSlotServingsCommand(TestWeek, DayOfWeek.Monday, MealType.Dinner, SlotServings.From(8));

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();
		result.Value.Slots.First(s => s.Day == "Monday").Servings.Should().Be(8);
		await plans.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_NonExistentPlan_ThrowsEntityNotFoundException()
	{
		plans.GetByOwnerAndWeekAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<WeekIdentifier>(), Arg.Any<CancellationToken>())
			.Returns<WeeklyPlan>(_ => throw new EntityNotFoundException(nameof(WeeklyPlan), "2026-W15"));

		var command = new AdjustSlotServingsCommand(TestWeek, DayOfWeek.Monday, MealType.Dinner, SlotServings.From(6));

		var act = () => handler.HandleAsync(command);

		await act.Should().ThrowAsync<EntityNotFoundException>();
	}
}
