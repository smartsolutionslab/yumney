using FluentAssertions;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.TestBuilders.MealPlan;
using Xunit;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.Tests.WeeklyPlan;

#pragma warning disable SA1601
public partial class WeeklyPlanTests
#pragma warning restore SA1601
{
	[Fact]
	public void Create_DefaultMode_AllSlotsDinner()
	{
		var plan = WeeklyPlanBuilder.A().Build();

		plan.IsExtendedMode.Should().BeFalse();
		plan.Slots.Should().OnlyContain(s => s.MealType == MealType.Dinner);
	}

	[Fact]
	public void EnableExtendedMode_Adds21Slots()
	{
		var plan = WeeklyPlanBuilder.A().InExtendedMode().Build();

		plan.IsExtendedMode.Should().BeTrue();
		plan.Slots.Should().HaveCount(21);
	}

	[Fact]
	public void EnableExtendedMode_PreservesDinnerRecipes()
	{
		var plan = WeeklyPlanBuilder.A().Build();
		var recipe = Recipe();
		plan.AssignRecipe(DayOfWeek.Monday, recipe);

		plan.EnableExtendedMode();

		plan.Slots.First(slot => slot.Day == DayOfWeek.Monday && slot.MealType == MealType.Dinner).Recipe!.Title.Should().Be(recipe.Title);
	}

	[Fact]
	public void EnableExtendedMode_AlreadyExtended_NoOp()
	{
		var plan = WeeklyPlanBuilder.A().InExtendedMode().Build();

		plan.EnableExtendedMode();

		plan.Slots.Should().HaveCount(21);
	}

	[Fact]
	public void DisableExtendedMode_ShowsOnlyDinner()
	{
		var plan = WeeklyPlanBuilder.A().InExtendedMode().Build();

		plan.DisableExtendedMode();

		plan.IsExtendedMode.Should().BeFalse();
		plan.GetVisibleSlots().Should().HaveCount(7);
		plan.GetVisibleSlots().Should().OnlyContain(s => s.MealType == MealType.Dinner);
	}

	[Fact]
	public void DisableExtendedMode_PreservesAllData()
	{
		var plan = WeeklyPlanBuilder.A().InExtendedMode().Build();
		var cereal = Recipe("Cereal");
		plan.AssignRecipe(DayOfWeek.Monday, cereal, MealType.Breakfast);

		plan.DisableExtendedMode();

		plan.Slots.Should().HaveCount(21);
		plan.Slots.First(slot => slot.Day == DayOfWeek.Monday && slot.MealType == MealType.Breakfast).Recipe!.Title.Should().Be(cereal.Title);
	}

	[Fact]
	public void AssignRecipe_BreakfastSlot_InExtendedMode()
	{
		var plan = WeeklyPlanBuilder.A().InExtendedMode().Build();
		var pancakes = Recipe("Pancakes");

		plan.AssignRecipe(DayOfWeek.Tuesday, pancakes, MealType.Breakfast);

		plan.Slots.First(slot => slot.Day == DayOfWeek.Tuesday && slot.MealType == MealType.Breakfast).Recipe!.Title.Should().Be(pancakes.Title);
	}

	[Fact]
	public void GetVisibleSlots_ExtendedMode_ReturnsAll21()
	{
		var plan = WeeklyPlanBuilder.A().InExtendedMode().Build();

		plan.GetVisibleSlots().Should().HaveCount(21);
	}

	[Fact]
	public void GetVisibleSlots_DefaultMode_Returns7()
	{
		var plan = WeeklyPlanBuilder.A().Build();

		plan.GetVisibleSlots().Should().HaveCount(7);
	}

	[Fact]
	public void AssignRecipe_BreakfastInDefaultMode_ThrowsEntityNotFoundException()
	{
		var plan = WeeklyPlanBuilder.A().Build();

		var act = () => plan.AssignRecipe(DayOfWeek.Monday, Recipe("Cereal"), MealType.Breakfast);

		act.Should().Throw<EntityNotFoundException>();
	}

	[Fact]
	public void ClearSlot_BreakfastInDefaultMode_ThrowsEntityNotFoundException()
	{
		var plan = WeeklyPlanBuilder.A().Build();

		var act = () => plan.ClearSlot(DayOfWeek.Monday, MealType.Breakfast);

		act.Should().Throw<EntityNotFoundException>();
	}

	[Fact]
	public void SwapSlots_BreakfastInExtendedMode_Works()
	{
		var plan = WeeklyPlanBuilder.A().InExtendedMode().Build();
		var pancakes = Recipe("Pancakes");
		plan.AssignRecipe(DayOfWeek.Monday, pancakes, MealType.Breakfast);

		plan.SwapSlots(DayOfWeek.Monday, DayOfWeek.Tuesday, MealType.Breakfast);

		plan.Slots.First(slot => slot.Day == DayOfWeek.Monday && slot.MealType == MealType.Breakfast).IsEmpty.Should().BeTrue();
		plan.Slots.First(slot => slot.Day == DayOfWeek.Tuesday && slot.MealType == MealType.Breakfast).Recipe!.Title.Should().Be(pancakes.Title);
	}

	[Fact]
	public void DisableExtendedMode_NotExtended_NoOp()
	{
		var plan = WeeklyPlanBuilder.A().Build();

		plan.DisableExtendedMode();

		plan.IsExtendedMode.Should().BeFalse();
		plan.Slots.Should().HaveCount(7);
	}
}
