using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Tests.Commands;

public class CookWithLeftoversCommandHandlerTests
{
	private readonly IWeeklyPlanRepository plans = Substitute.For<IWeeklyPlanRepository>();
	private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
	private readonly CookWithLeftoversCommandHandler handler;

	public CookWithLeftoversCommandHandlerTests()
	{
		currentUser.UserId.Returns("user-123");
		handler = new CookWithLeftoversCommandHandler(plans, currentUser);
	}

	[Fact]
	public async Task HandleAsync_Cook8Eat4_CreatesRecipeAndLeftover()
	{
		plans.FindForUpdateAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<WeekIdentifier>(), Arg.Any<CancellationToken>())
			.Returns((WeeklyPlan?)null);

		var command = new CookWithLeftoversCommand(WeekIdentifier.From(2026, 15), DayOfWeek.Monday, SlotRecipeReference.From(Guid.NewGuid(), "Bolognese"), SlotServings.From(8), SlotServings.From(4), DayOfWeek.Wednesday);

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();

		var monday = result.Value.Slots.First(s => s.Day == "Monday");
		monday.ContentType.Should().Be("Recipe");
		monday.Servings.Should().Be(8);

		var wednesday = result.Value.Slots.First(s => s.Day == "Wednesday");
		wednesday.ContentType.Should().Be("Leftover");
		wednesday.LeftoverSourceDay.Should().Be("Monday");
		wednesday.Servings.Should().Be(4);
	}

	[Fact]
	public async Task HandleAsync_NoLeftoverWhenEqual_OnlyRecipe()
	{
		plans.FindForUpdateAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<WeekIdentifier>(), Arg.Any<CancellationToken>())
			.Returns((WeeklyPlan?)null);

		var command = new CookWithLeftoversCommand(WeekIdentifier.From(2026, 15), DayOfWeek.Monday, SlotRecipeReference.From(Guid.NewGuid(), "Pasta"), SlotServings.From(4), SlotServings.From(4), DayOfWeek.Tuesday);

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();
		result.Value.Slots.First(s => s.Day == "Monday").ContentType.Should().Be("Recipe");
		result.Value.Slots.First(s => s.Day == "Tuesday").ContentType.Should().Be("Empty");
	}

	[Fact]
	public void CreateCommand_InvalidServings_ThrowsGuardException()
	{
		var act = () => new CookWithLeftoversCommand(
			WeekIdentifier.From(2026, 15),
			DayOfWeek.Monday,
			SlotRecipeReference.From(Guid.NewGuid(), "Pasta"),
			SlotServings.From(0),
			SlotServings.From(0),
			DayOfWeek.Tuesday);

		act.Should().Throw<GuardException>();
	}
}
