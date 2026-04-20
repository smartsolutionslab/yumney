using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;
using static SmartSolutionsLab.Yumney.MealPlan.Application.Tests.MealPlanTestFixture;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Tests.Commands;

public class AssignRecipeCommandHandlerTests
{
	private readonly IWeeklyPlanRepository plans = Substitute.For<IWeeklyPlanRepository>();
	private readonly IMealPlanUnitOfWork unitOfWork = Substitute.For<IMealPlanUnitOfWork>();
	private readonly AssignRecipeCommandHandler handler;

	public AssignRecipeCommandHandlerTests()
	{
		unitOfWork.Plans.Returns(plans);
		handler = new AssignRecipeCommandHandler(unitOfWork, CreateCurrentUser());
	}

	[Fact]
	public async Task HandleAsync_NoPlanExists_CreatesNewPlanAndAssigns()
	{
		plans.FindForUpdateAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<WeekIdentifier>(), Arg.Any<CancellationToken>())
			.Returns((WeeklyPlan?)null);

		var command = new AssignRecipeCommand(TestWeek, DayOfWeek.Monday, Recipe());

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();
		result.Value.Slots.Should().HaveCount(7);
		await plans.Received(1).AddAsync(Arg.Any<WeeklyPlan>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_PlanExists_UpdatesExisting()
	{
		var existing = CreatePlan();

		plans.FindForUpdateAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<WeekIdentifier>(), Arg.Any<CancellationToken>())
			.Returns(existing);

		var command = new AssignRecipeCommand(TestWeek, DayOfWeek.Wednesday, Recipe("Steak"), Servings: SlotServings.From(6));

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();
		await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
		await plans.DidNotReceive().AddAsync(Arg.Any<WeeklyPlan>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_ReturnsCorrectWeekInDto()
	{
		plans.FindForUpdateAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<WeekIdentifier>(), Arg.Any<CancellationToken>())
			.Returns((WeeklyPlan?)null);

		var command = new AssignRecipeCommand(TestWeek, DayOfWeek.Friday, Recipe("Fish"));

		var result = await handler.HandleAsync(command);

		result.Value.Week.Should().Be("2026-W15");
	}

	[Fact]
	public async Task HandleAsync_WithBreakfastMealType_AssignsToBreakfastSlot()
	{
		var existing = CreatePlan();
		existing.EnableExtendedMode();

		plans.FindForUpdateAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<WeekIdentifier>(), Arg.Any<CancellationToken>())
			.Returns(existing);

		var command = new AssignRecipeCommand(TestWeek, DayOfWeek.Monday, Recipe("Pancakes"), MealType.Breakfast);

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();
		result.Value.Slots.Should().Contain(s => s.RecipeTitle == "Pancakes" && s.MealType == "Breakfast");
	}
}
