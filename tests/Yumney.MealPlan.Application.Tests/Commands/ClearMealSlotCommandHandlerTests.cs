using FluentAssertions;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;
using Xunit;
using static SmartSolutionsLab.Yumney.MealPlan.Application.Tests.MealPlanTestFixture;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Tests.Commands;

public class ClearMealSlotCommandHandlerTests
{
	private readonly FakeMealPlanEventStore eventStore = new();
	private readonly ClearMealSlotCommandHandler handler;

	public ClearMealSlotCommandHandlerTests()
	{
		handler = new ClearMealSlotCommandHandler(eventStore, CreateCurrentUser());
	}

	[Fact]
	public async Task HandleAsync_ClearsSlotAndSaves()
	{
		SeedPlanWithRecipe(eventStore, DayOfWeek.Wednesday);

		var command = new ClearMealSlotCommand(TestWeek, DayOfWeek.Wednesday);

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();
		result.Value.Slots.First(s => s.Day == "Wednesday").IsEmpty.Should().BeTrue();
		eventStore.SaveCount.Should().Be(1);
	}
}
