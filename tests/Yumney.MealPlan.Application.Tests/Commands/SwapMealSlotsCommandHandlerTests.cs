using FluentAssertions;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;
using static SmartSolutionsLab.Yumney.MealPlan.Application.Tests.MealPlanTestFixture;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Tests.Commands;

public class SwapMealSlotsCommandHandlerTests
{
	private readonly FakeMealPlanEventStore eventStore = new();
	private readonly SwapMealSlotsCommandHandler handler;

	public SwapMealSlotsCommandHandlerTests()
	{
		handler = new SwapMealSlotsCommandHandler(eventStore, CreateCurrentUser());
	}

	[Fact]
	public async Task HandleAsync_SwapsAndSaves()
	{
		SeedPlanWithRecipe(eventStore);

		var command = new SwapMealSlotsCommand(TestWeek, DayOfWeek.Monday, DayOfWeek.Wednesday);

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();
		result.Value.Slots.First(s => s.Day == "Monday").IsEmpty.Should().BeTrue();
		result.Value.Slots.First(s => s.Day == "Wednesday").RecipeTitle.Should().Be("Pasta");
		eventStore.SaveCount.Should().Be(1);
	}

	[Fact]
	public async Task HandleAsync_NoPlan_ThrowsEntityNotFoundException()
	{
		var command = new SwapMealSlotsCommand(TestWeek, DayOfWeek.Monday, DayOfWeek.Tuesday);

		var act = () => handler.HandleAsync(command);

		await act.Should().ThrowAsync<EntityNotFoundException>();
	}
}
