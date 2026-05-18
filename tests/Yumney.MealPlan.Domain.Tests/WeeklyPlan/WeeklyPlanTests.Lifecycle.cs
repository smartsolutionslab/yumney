using FluentAssertions;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.TestBuilders.MealPlan;
using Xunit;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.Tests.WeeklyPlan;

#pragma warning disable SA1601
public partial class WeeklyPlanTests
#pragma warning restore SA1601
{
	[Fact]
	public void AdjustServings_ChangesSlotServings()
	{
		var plan = WeeklyPlanBuilder.A().WithDefaultServings(4).Build();
		plan.AssignRecipe(DayOfWeek.Monday, Recipe());
		var newServings = SlotServings.From(8);

		plan.AdjustServings(DayOfWeek.Monday, newServings);

		plan.Slots.First(slot => slot.Day == DayOfWeek.Monday).Servings.Should().Be(newServings);
	}

	[Fact]
	public void AdjustServings_DefaultServingsPreservedOnOtherSlots()
	{
		var defaultServings = SlotServings.From(4);
		var plan = WeeklyPlanBuilder.A().WithDefaultServings(defaultServings).Build();

		plan.AdjustServings(DayOfWeek.Monday, SlotServings.From(6));

		plan.Slots.First(slot => slot.Day == DayOfWeek.Tuesday).Servings.Should().Be(defaultServings);
	}

	[Fact]
	public void SwapSlots_PreservesServings()
	{
		var defaultServings = SlotServings.From(4);
		var overrideServings = SlotServings.From(8);
		var plan = WeeklyPlanBuilder.A().WithDefaultServings(defaultServings).Build();
		plan.AssignRecipe(DayOfWeek.Monday, Recipe(), servings: overrideServings);

		plan.SwapSlots(DayOfWeek.Monday, DayOfWeek.Tuesday);

		plan.Slots.First(slot => slot.Day == DayOfWeek.Tuesday).Servings.Should().Be(overrideServings);
		plan.Slots.First(slot => slot.Day == DayOfWeek.Monday).Servings.Should().Be(defaultServings);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void AdjustServings_ZeroOrNegative_ThrowsGuardException(int servings)
	{
		var plan = WeeklyPlanBuilder.A().Build();

		var act = () => plan.AdjustServings(DayOfWeek.Monday, SlotServings.From(servings));

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void MarkAsCooked_SetsState()
	{
		var plan = WeeklyPlanBuilder.A().Build();
		plan.AssignRecipe(DayOfWeek.Monday, Recipe());

		plan.MarkAsCooked(DayOfWeek.Monday);

		plan.Slots.First(slot => slot.Day == DayOfWeek.Monday).State.Should().Be(MealState.Cooked);
	}

	[Fact]
	public void MarkAsSkipped_SetsState()
	{
		var plan = WeeklyPlanBuilder.A().Build();
		plan.AssignRecipe(DayOfWeek.Monday, Recipe());

		plan.MarkAsSkipped(DayOfWeek.Monday);

		plan.Slots.First(slot => slot.Day == DayOfWeek.Monday).State.Should().Be(MealState.Skipped);
	}

	[Fact]
	public void ResetToPlanned_ResetsState()
	{
		var plan = WeeklyPlanBuilder.A().Build();
		plan.AssignRecipe(DayOfWeek.Monday, Recipe());
		plan.MarkAsCooked(DayOfWeek.Monday);

		plan.ResetToPlanned(DayOfWeek.Monday);

		plan.Slots.First(slot => slot.Day == DayOfWeek.Monday).State.Should().Be(MealState.Planned);
	}

	[Fact]
	public void Create_AllSlotsStartAsPlanned()
	{
		var plan = WeeklyPlanBuilder.A().Build();

		plan.Slots.Should().OnlyContain(s => s.State == MealState.Planned);
	}

	[Fact]
	public void GetUnconfirmedPastMeals_ReturnsOnlyPlannedRecipesBeforeToday()
	{
		var plan = WeeklyPlanBuilder.A().Build();
		plan.AssignRecipe(DayOfWeek.Monday, Recipe());
		plan.AssignRecipe(DayOfWeek.Tuesday, Recipe("Steak"));
		plan.MarkAsCooked(DayOfWeek.Monday);

		var unconfirmed = plan.GetUnconfirmedPastMeals(DayOfWeek.Wednesday);

		unconfirmed.Should().HaveCount(1);
		unconfirmed[0].Day.Should().Be(DayOfWeek.Tuesday);
	}

	[Fact]
	public void GetUnconfirmedPastMeals_ExcludesFreetextAndLeftover()
	{
		var plan = WeeklyPlanBuilder.A().Build();
		var steak = Recipe("Steak");
		plan.SetFreetext(DayOfWeek.Monday, FreetextLabel.From("Eating out"));
		plan.AssignRecipe(DayOfWeek.Tuesday, steak);

		var unconfirmed = plan.GetUnconfirmedPastMeals(DayOfWeek.Wednesday);

		unconfirmed.Should().HaveCount(1);
		unconfirmed[0].Recipe!.Title.Should().Be(steak.Title);
	}
}
