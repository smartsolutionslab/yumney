using FluentAssertions;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;
using static SmartSolutionsLab.Yumney.MealPlan.Application.Tests.MealPlanTestFixture;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Tests.Commands;

public class AdjustSlotServingsCommandHandlerTests
{
	private readonly FakeMealPlanEventStore eventStore = new();
	private readonly AdjustSlotServingsCommandHandler handler;

	public AdjustSlotServingsCommandHandlerTests()
	{
		handler = new AdjustSlotServingsCommandHandler(eventStore, CreateCurrentUser());
	}

	[Fact]
	public async Task HandleAsync_AdjustsServingsAndSaves()
	{
		SeedPlanWithRecipe(eventStore);

		var command = new AdjustSlotServingsCommand(TestWeek, DayOfWeek.Monday, MealType.Dinner, SlotServings.From(8));

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();
		result.Value.Slots.First(s => s.Day == "Monday").Servings.Should().Be(8);
		eventStore.SaveCount.Should().Be(1);
	}

	[Fact]
	public async Task HandleAsync_NonExistentPlan_ThrowsEntityNotFoundException()
	{
		var command = new AdjustSlotServingsCommand(TestWeek, DayOfWeek.Monday, MealType.Dinner, SlotServings.From(6));

		var act = () => handler.HandleAsync(command);

		await act.Should().ThrowAsync<EntityNotFoundException>();
	}
}
