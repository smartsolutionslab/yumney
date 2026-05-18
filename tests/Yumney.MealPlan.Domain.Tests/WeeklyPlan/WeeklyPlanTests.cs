using FluentAssertions;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.TestBuilders.MealPlan;
using Xunit;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.Tests.WeeklyPlan;

#pragma warning disable SA1601
public partial class WeeklyPlanTests
#pragma warning restore SA1601
{
	[Fact]
	public void Create_ValidParams_Returns7EmptySlots()
	{
		var plan = WeeklyPlanBuilder.A().Build();

		plan.Owner.Should().Be(OwnerIdentifier.From("user-123"));
		plan.Week.Should().Be(WeekIdentifier.From(2026, 17));
		plan.Slots.Should().HaveCount(7);
		plan.Slots.Should().OnlyContain(s => s.IsEmpty);
	}

	[Fact]
	public void Create_SetsDefaultServings()
	{
		var plan = WeeklyPlanBuilder.A().WithDefaultServings(6).Build();

		plan.Slots.Should().OnlyContain(s => s.Servings.Value == 6);
	}

	[Fact]
	public void AssignRecipe_FillsSlot()
	{
		var plan = WeeklyPlanBuilder.A().Build();
		var recipe = Recipe("Spaghetti Bolognese");

		plan.AssignRecipe(DayOfWeek.Monday, recipe);

		var monday = plan.Slots.First(slot => slot.Day == DayOfWeek.Monday);
		monday.IsEmpty.Should().BeFalse();
		monday.Recipe!.Identifier.Should().Be(recipe.Identifier);
		monday.Recipe.Title.Should().Be(recipe.Title);
	}

	[Fact]
	public void AssignRecipe_WithServings_OverridesDefault()
	{
		var plan = WeeklyPlanBuilder.A().Build();
		var servings = SlotServings.From(8);

		plan.AssignRecipe(DayOfWeek.Monday, Recipe(), servings: servings);

		plan.Slots.First(slot => slot.Day == DayOfWeek.Monday).Servings.Should().Be(servings);
	}

	[Fact]
	public void ClearSlot_RemovesRecipe()
	{
		var plan = WeeklyPlanBuilder.A().Build();
		plan.AssignRecipe(DayOfWeek.Wednesday, Recipe("Chicken"));

		plan.ClearSlot(DayOfWeek.Wednesday);

		plan.Slots.First(slot => slot.Day == DayOfWeek.Wednesday).IsEmpty.Should().BeTrue();
	}

	[Fact]
	public void SwapSlots_SwapsTwoMeals()
	{
		var plan = WeeklyPlanBuilder.A().Build();
		var pasta = Recipe("Pasta");
		var steak = Recipe("Steak");
		plan.AssignRecipe(DayOfWeek.Monday, pasta);
		plan.AssignRecipe(DayOfWeek.Friday, steak);

		plan.SwapSlots(DayOfWeek.Monday, DayOfWeek.Friday);

		plan.Slots.First(slot => slot.Day == DayOfWeek.Monday).Recipe!.Title.Should().Be(steak.Title);
		plan.Slots.First(slot => slot.Day == DayOfWeek.Friday).Recipe!.Title.Should().Be(pasta.Title);
	}

	[Fact]
	public void SwapSlots_WithOneEmpty_MovesRecipe()
	{
		var plan = WeeklyPlanBuilder.A().Build();
		var recipe = Recipe();
		plan.AssignRecipe(DayOfWeek.Monday, recipe);

		plan.SwapSlots(DayOfWeek.Monday, DayOfWeek.Tuesday);

		plan.Slots.First(slot => slot.Day == DayOfWeek.Monday).IsEmpty.Should().BeTrue();
		plan.Slots.First(slot => slot.Day == DayOfWeek.Tuesday).Recipe!.Title.Should().Be(recipe.Title);
	}

	[Fact]
	public void AssignRecipe_EmptyTitle_ThrowsGuardException()
	{
		var plan = WeeklyPlanBuilder.A().Build();

		var act = () => plan.AssignRecipe(
			DayOfWeek.Monday,
			SlotRecipeReference.From(
				SlotRecipeIdentifier.New(),
				SlotRecipeTitle.From(string.Empty)));

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void WeekIdentifier_CurrentReturnsValue()
	{
		var current = WeekIdentifier.Current();

		current.Year.Should().BeGreaterThan(2024);
		current.WeekNumber.Should().BeInRange(1, 53);
	}

	[Fact]
	public void WeekIdentifier_FromDate_CorrectWeek()
	{
		var week = WeekIdentifier.FromDate(new DateOnly(2026, 4, 13));

		week.Year.Should().Be(2026);
		week.WeekNumber.Should().Be(16);
	}

	[Fact]
	public void AssignRecipe_OverwritesExistingRecipe()
	{
		var plan = WeeklyPlanBuilder.A().Build();
		plan.AssignRecipe(DayOfWeek.Monday, Recipe("Pasta"));

		var steak = Recipe("Steak");
		plan.AssignRecipe(DayOfWeek.Monday, steak);

		var monday = plan.Slots.First(slot => slot.Day == DayOfWeek.Monday);
		monday.Recipe!.Identifier.Should().Be(steak.Identifier);
		monday.Recipe.Title.Should().Be(steak.Title);
	}

	[Fact]
	public void SwapSlots_BothEmpty_NoOp()
	{
		var plan = WeeklyPlanBuilder.A().Build();

		plan.SwapSlots(DayOfWeek.Monday, DayOfWeek.Tuesday);

		plan.Slots.First(slot => slot.Day == DayOfWeek.Monday).IsEmpty.Should().BeTrue();
		plan.Slots.First(slot => slot.Day == DayOfWeek.Tuesday).IsEmpty.Should().BeTrue();
	}

	[Fact]
	public void ClearSlot_AlreadyEmpty_NoError()
	{
		var plan = WeeklyPlanBuilder.A().Build();

		var act = () => plan.ClearSlot(DayOfWeek.Friday);

		act.Should().NotThrow();
	}

	[Fact]
	public void WeekIdentifier_ToString_ReturnsIsoFormat()
	{
		var week = WeekIdentifier.From(2026, 3);

		week.ToString().Should().Be("2026-W03");
	}

	[Fact]
	public void Slots_ContainAllSevenDays()
	{
		var plan = WeeklyPlanBuilder.A().Build();

		var days = plan.Slots.Select(slot => slot.Day).ToList();
		days.Should().Contain(DayOfWeek.Monday);
		days.Should().Contain(DayOfWeek.Tuesday);
		days.Should().Contain(DayOfWeek.Wednesday);
		days.Should().Contain(DayOfWeek.Thursday);
		days.Should().Contain(DayOfWeek.Friday);
		days.Should().Contain(DayOfWeek.Saturday);
		days.Should().Contain(DayOfWeek.Sunday);
	}

	private static SlotRecipeReference Recipe(string title = "Pasta") =>
		SlotRecipeReference.From(SlotRecipeIdentifier.New(), SlotRecipeTitle.From(title));

	private static SlotRecipeReference Recipe(Guid id, string title) =>
		SlotRecipeReference.From(id, title);
}
