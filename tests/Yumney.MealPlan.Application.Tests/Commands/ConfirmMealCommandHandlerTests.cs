using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using Xunit;
using static SmartSolutionsLab.Yumney.MealPlan.Application.Tests.MealPlanTestFixture;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Tests.Commands;

public class ConfirmMealCommandHandlerTests
{
	private readonly FakeMealPlanEventStore eventStore = new();
	private readonly IRecipeIngredientLookup recipeIngredients = Substitute.For<IRecipeIngredientLookup>();
	private readonly ConfirmMealCommandHandler handler;

	public ConfirmMealCommandHandlerTests()
	{
		recipeIngredients.LookupAsync(Arg.Any<SlotRecipeIdentifier>(), Arg.Any<CancellationToken>())
			.Returns(Array.Empty<RecipeIngredientLookupResult>());
		handler = new ConfirmMealCommandHandler(eventStore, CreateCurrentUser(), recipeIngredients);
	}

	[Fact]
	public async Task HandleAsync_MarkAsCooked_SetsSlotStateToCooked()
	{
		SeedPlanWithRecipe(eventStore);
		var command = new ConfirmMealCommand(TestWeek, DayOfWeek.Monday, MealType.Dinner, MealState.Cooked);

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();
		var slot = result.Value.Slots.First(slot => slot.Day == "Monday");
		slot.State.Should().Be("Cooked");
		eventStore.SaveCount.Should().Be(1);
	}

	[Fact]
	public async Task HandleAsync_MarkAsSkipped_SetsSlotStateToSkipped()
	{
		SeedPlanWithRecipe(eventStore);
		var command = new ConfirmMealCommand(TestWeek, DayOfWeek.Monday, MealType.Dinner, MealState.Skipped);

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();
		var slot = result.Value.Slots.First(slot => slot.Day == "Monday");
		slot.State.Should().Be("Skipped");
	}

	[Fact]
	public async Task HandleAsync_ResetToPlanned_SetsSlotStateBackToPlanned()
	{
		var plan = SeedPlanWithRecipe(eventStore);
		plan.MarkAsCooked(DayOfWeek.Monday, MealType.Dinner);

		var command = new ConfirmMealCommand(TestWeek, DayOfWeek.Monday, MealType.Dinner, MealState.Planned);

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();
		var slot = result.Value.Slots.First(slot => slot.Day == "Monday");
		slot.State.Should().Be("Planned");
	}
}
