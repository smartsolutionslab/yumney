using FluentAssertions;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;
using static SmartSolutionsLab.Yumney.MealPlan.Application.Tests.MealPlanTestFixture;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Tests.Commands;

public class CookWithLeftoversCommandHandlerTests
{
	private readonly FakeMealPlanEventStore eventStore = new();
	private readonly CookWithLeftoversCommandHandler handler;

	public CookWithLeftoversCommandHandlerTests()
	{
		handler = new CookWithLeftoversCommandHandler(eventStore, CreateCurrentUser());
	}

	[Fact]
	public async Task HandleAsync_Cook8Eat4_CreatesRecipeAndLeftover()
	{
		var command = new CookWithLeftoversCommand(TestWeek, DayOfWeek.Monday, Recipe("Bolognese"), SlotServings.From(8), SlotServings.From(4), DayOfWeek.Wednesday);

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();

		var monday = result.Value.Slots.First(slot => slot.Day == "Monday");
		monday.ContentType.Should().Be("Recipe");
		monday.Servings.Should().Be(8);

		var wednesday = result.Value.Slots.First(slot => slot.Day == "Wednesday");
		wednesday.ContentType.Should().Be("Leftover");
		wednesday.LeftoverSourceDay.Should().Be("Monday");
		wednesday.Servings.Should().Be(4);
	}

	[Fact]
	public async Task HandleAsync_NoLeftoverWhenEqual_OnlyRecipe()
	{
		var command = new CookWithLeftoversCommand(TestWeek, DayOfWeek.Monday, Recipe(), SlotServings.From(4), SlotServings.From(4), DayOfWeek.Tuesday);

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();
		result.Value.Slots.First(slot => slot.Day == "Monday").ContentType.Should().Be("Recipe");
		result.Value.Slots.First(slot => slot.Day == "Tuesday").ContentType.Should().Be("Empty");
	}

	[Fact]
	public void CreateCommand_InvalidServings_ThrowsGuardException()
	{
		var act = () => new CookWithLeftoversCommand(
			TestWeek,
			DayOfWeek.Monday,
			Recipe(),
			SlotServings.From(0),
			SlotServings.From(0),
			DayOfWeek.Tuesday);

		act.Should().Throw<GuardException>();
	}
}
