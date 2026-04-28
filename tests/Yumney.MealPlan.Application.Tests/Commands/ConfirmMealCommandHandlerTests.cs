using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;
using static SmartSolutionsLab.Yumney.MealPlan.Application.Tests.MealPlanTestFixture;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Tests.Commands;

public class ConfirmMealCommandHandlerTests
{
	private readonly FakeMealPlanEventStore eventStore = new();
	private readonly IRecipeIngredientProvider ingredientProvider = Substitute.For<IRecipeIngredientProvider>();
	private readonly ConfirmMealCommandHandler handler;

	public ConfirmMealCommandHandlerTests()
	{
		ingredientProvider.GetIngredientsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
			.Returns(Array.Empty<RecipeIngredientInfo>());
		handler = new ConfirmMealCommandHandler(eventStore, CreateCurrentUser(), ingredientProvider);
	}

	[Fact]
	public async Task HandleAsync_MarkAsCooked_SetsSlotStateToCooked()
	{
		SeedPlanWithRecipe(eventStore);
		var command = new ConfirmMealCommand(TestWeek, DayOfWeek.Monday, MealType.Dinner, MealState.Cooked);

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();
		var slot = result.Value.Slots.First(s => s.Day == "Monday");
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
		var slot = result.Value.Slots.First(s => s.Day == "Monday");
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
		var slot = result.Value.Slots.First(s => s.Day == "Monday");
		slot.State.Should().Be("Planned");
	}
}
