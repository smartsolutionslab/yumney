using FluentAssertions;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using Xunit;
using static SmartSolutionsLab.Yumney.MealPlan.Application.Tests.MealPlanTestFixture;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Tests.Commands;

public class AssignRecipeCommandHandlerTests
{
	private readonly FakeMealPlanEventStore eventStore = new();
	private readonly AssignRecipeCommandHandler handler;

	public AssignRecipeCommandHandlerTests()
	{
		handler = new AssignRecipeCommandHandler(eventStore, CreateCurrentUser());
	}

	[Fact]
	public async Task HandleAsync_NoPlanExists_CreatesNewPlanAndAssigns()
	{
		var command = new AssignRecipeCommand(TestWeek, DayOfWeek.Monday, Recipe());

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();
		result.Value.Slots.Should().HaveCount(7);
		eventStore.SaveCount.Should().Be(1);
	}

	[Fact]
	public async Task HandleAsync_PlanExists_UpdatesExisting()
	{
		SeedPlanWithRecipe(eventStore);

		var command = new AssignRecipeCommand(TestWeek, DayOfWeek.Wednesday, Recipe("Steak"), Servings: SlotServings.From(6));

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();
		eventStore.SaveCount.Should().Be(1);
	}

	[Fact]
	public async Task HandleAsync_ReturnsCorrectWeekInDto()
	{
		var command = new AssignRecipeCommand(TestWeek, DayOfWeek.Friday, Recipe("Fish"));

		var result = await handler.HandleAsync(command);

		result.Value.Week.Should().Be("2026-W15");
	}

	[Fact]
	public async Task HandleAsync_WithBreakfastMealType_AssignsToBreakfastSlot()
	{
		var existing = CreatePlan();
		existing.EnableExtendedMode();
		eventStore.Seed(existing);

		var command = new AssignRecipeCommand(TestWeek, DayOfWeek.Monday, Recipe("Pancakes"), MealType.Breakfast);

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();
		result.Value.Slots.Should().Contain(s => s.RecipeTitle == "Pancakes" && s.MealType == "Breakfast");
	}
}
