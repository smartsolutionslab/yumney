using FluentAssertions;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using Xunit;
using static SmartSolutionsLab.Yumney.MealPlan.Application.Tests.MealPlanTestFixture;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Tests.Commands;

public class CopyPlanToWeekCommandHandlerTests
{
	private static readonly WeekIdentifier SourceWeek = WeekIdentifier.From(2026, 10);
	private static readonly WeekIdentifier TargetWeek = WeekIdentifier.From(2026, 20);

	private readonly FakeMealPlanEventStore eventStore = new();
	private readonly CopyPlanToWeekCommandHandler handler;

	public CopyPlanToWeekCommandHandlerTests()
	{
		handler = new CopyPlanToWeekCommandHandler(eventStore, CreateCurrentUser());
	}

	[Fact]
	public async Task HandleAsync_SourceMissing_ReturnsSourcePlanNotFound()
	{
		var result = await handler.HandleAsync(new CopyPlanToWeekCommand(SourceWeek, TargetWeek));

		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(CopyPlanToWeekErrors.SourcePlanNotFound);
	}

	[Fact]
	public async Task HandleAsync_SameSourceAndTarget_ReturnsSameWeekError()
	{
		var result = await handler.HandleAsync(new CopyPlanToWeekCommand(SourceWeek, SourceWeek));

		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(CopyPlanToWeekErrors.SameWeek);
	}

	[Fact]
	public async Task HandleAsync_RecipeSlotsCopiedToTarget()
	{
		var source = WeeklyPlan.Create(TestOwner, SourceWeek);
		source.AssignRecipe(DayOfWeek.Monday, Recipe("Pasta"));
		source.AssignRecipe(DayOfWeek.Wednesday, Recipe("Soup"));
		eventStore.Seed(source);

		var result = await handler.HandleAsync(new CopyPlanToWeekCommand(SourceWeek, TargetWeek));

		result.IsSuccess.Should().BeTrue();
		var saved = eventStore.LastSavedPlan!;
		saved.Week.Should().Be(TargetWeek);
		saved.Slots.Where(slot => slot.ContentType == SlotContentType.Recipe).Should().HaveCount(2);
		saved.Slots.Should().Contain(s => s.Recipe != null && s.Recipe.Title.Value == "Pasta" && s.Day == DayOfWeek.Monday);
		saved.Slots.Should().Contain(s => s.Recipe != null && s.Recipe.Title.Value == "Soup" && s.Day == DayOfWeek.Wednesday);
	}

	[Fact]
	public async Task HandleAsync_CookedStateNotCarriedOver()
	{
		var source = WeeklyPlan.Create(TestOwner, SourceWeek);
		source.AssignRecipe(DayOfWeek.Monday, Recipe("Pasta"));
		source.MarkAsCooked(DayOfWeek.Monday);
		eventStore.Seed(source);

		var result = await handler.HandleAsync(new CopyPlanToWeekCommand(SourceWeek, TargetWeek));

		result.IsSuccess.Should().BeTrue();
		var copied = eventStore.LastSavedPlan!.Slots.Single(slot => slot.Day == DayOfWeek.Monday && slot.MealType == MealType.Dinner);
		copied.State.Should().Be(MealState.Planned);
	}

	[Fact]
	public async Task HandleAsync_LeftoverSlotsAreDropped()
	{
		var source = WeeklyPlan.Create(TestOwner, SourceWeek);
		source.AssignRecipe(DayOfWeek.Monday, Recipe("Pasta"));
		source.SetLeftover(DayOfWeek.Tuesday, DayOfWeek.Monday, MealType.Dinner, SlotRecipeTitle.From("Pasta"));
		eventStore.Seed(source);

		var result = await handler.HandleAsync(new CopyPlanToWeekCommand(SourceWeek, TargetWeek));

		result.IsSuccess.Should().BeTrue();
		eventStore.LastSavedPlan!.Slots
			.Where(slot => slot.ContentType == SlotContentType.Leftover)
			.Should().BeEmpty();
	}

	[Fact]
	public async Task HandleAsync_FreetextSlotsCopied()
	{
		var source = WeeklyPlan.Create(TestOwner, SourceWeek);
		source.SetFreetext(DayOfWeek.Friday, FreetextLabel.From("Date night out"));
		eventStore.Seed(source);

		var result = await handler.HandleAsync(new CopyPlanToWeekCommand(SourceWeek, TargetWeek));

		result.IsSuccess.Should().BeTrue();
		eventStore.LastSavedPlan!.Slots
			.Should().Contain(s =>
				s.ContentType == SlotContentType.Freetext
				&& s.FreetextLabel != null
				&& s.FreetextLabel.Value == "Date night out");
	}

	[Fact]
	public async Task HandleAsync_ExtendedModePropagated()
	{
		var source = WeeklyPlan.Create(TestOwner, SourceWeek);
		source.EnableExtendedMode();
		source.AssignRecipe(DayOfWeek.Monday, Recipe("Pancakes"), MealType.Breakfast);
		eventStore.Seed(source);

		var result = await handler.HandleAsync(new CopyPlanToWeekCommand(SourceWeek, TargetWeek));

		result.IsSuccess.Should().BeTrue();
		eventStore.LastSavedPlan!.IsExtendedMode.Should().BeTrue();
		eventStore.LastSavedPlan.Slots
			.Should().Contain(s => s.MealType == MealType.Breakfast && s.Recipe != null && s.Recipe.Title.Value == "Pancakes");
	}
}
