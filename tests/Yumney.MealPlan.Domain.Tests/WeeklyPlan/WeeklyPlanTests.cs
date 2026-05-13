using FluentAssertions;
using SmartSolutionsLab.Yumney.MealPlan.Domain.Tests.Builders;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.Tests.WeeklyPlan;

public class WeeklyPlanTests
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
		monday.Recipe!.RecipeIdentifier.Should().Be(recipe.RecipeIdentifier);
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
		monday.Recipe!.RecipeIdentifier.Should().Be(steak.RecipeIdentifier);
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

	[Fact]
	public void SetFreetext_SetsContentType()
	{
		var plan = WeeklyPlanBuilder.A().Build();
		var label = FreetextLabel.From("Eating out");

		plan.SetFreetext(DayOfWeek.Monday, label);

		var monday = plan.Slots.First(slot => slot.Day == DayOfWeek.Monday);
		monday.ContentType.Should().Be(SlotContentType.Freetext);
		monday.FreetextLabel.Should().Be(label);
		monday.IsEmpty.Should().BeFalse();
		monday.Recipe.Should().BeNull();
	}

	[Fact]
	public void SetFreetext_EmptyLabel_ThrowsGuardException()
	{
		var plan = WeeklyPlanBuilder.A().Build();

		var act = () => plan.SetFreetext(DayOfWeek.Monday, FreetextLabel.From(string.Empty));

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void SetLeftover_SetsContentTypeAndSource()
	{
		var plan = WeeklyPlanBuilder.A().Build();
		var bolognese = Recipe("Bolognese");
		plan.AssignRecipe(DayOfWeek.Monday, bolognese);

		plan.SetLeftover(DayOfWeek.Wednesday, DayOfWeek.Monday, MealType.Dinner, bolognese.Title);

		var wednesday = plan.Slots.First(slot => slot.Day == DayOfWeek.Wednesday);
		wednesday.ContentType.Should().Be(SlotContentType.Leftover);
		wednesday.LeftoverSourceDay.Should().Be(DayOfWeek.Monday);
		wednesday.LeftoverSourceMealType.Should().Be(MealType.Dinner);
		wednesday.LeftoverLabel!.Value.Should().Contain("Bolognese");
		wednesday.IsEmpty.Should().BeFalse();
	}

	[Fact]
	public void SetLeftover_EmptyTitle_ThrowsGuardException()
	{
		var plan = WeeklyPlanBuilder.A().Build();

		var act = () => plan.SetLeftover(DayOfWeek.Wednesday, DayOfWeek.Monday, MealType.Dinner, SlotRecipeTitle.From(string.Empty));

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void AssignRecipe_SetsContentTypeToRecipe()
	{
		var plan = WeeklyPlanBuilder.A().Build();

		plan.AssignRecipe(DayOfWeek.Tuesday, Recipe("Steak"));

		plan.Slots.First(slot => slot.Day == DayOfWeek.Tuesday).ContentType.Should().Be(SlotContentType.Recipe);
	}

	[Fact]
	public void ClearSlot_ResetsContentTypeToEmpty()
	{
		var plan = WeeklyPlanBuilder.A().Build();
		plan.SetFreetext(DayOfWeek.Monday, FreetextLabel.From("Pizza night"));

		plan.ClearSlot(DayOfWeek.Monday);

		var monday = plan.Slots.First(slot => slot.Day == DayOfWeek.Monday);
		monday.ContentType.Should().Be(SlotContentType.Empty);
		monday.FreetextLabel.Should().BeNull();
		monday.IsEmpty.Should().BeTrue();
	}

	[Fact]
	public void AssignRecipe_ClearsFreetextAndLeftoverFields()
	{
		var plan = WeeklyPlanBuilder.A().Build();
		plan.SetFreetext(DayOfWeek.Monday, FreetextLabel.From("Eating out"));

		plan.AssignRecipe(DayOfWeek.Monday, Recipe());

		var monday = plan.Slots.First(slot => slot.Day == DayOfWeek.Monday);
		monday.ContentType.Should().Be(SlotContentType.Recipe);
		monday.FreetextLabel.Should().BeNull();
		monday.LeftoverSourceDay.Should().BeNull();
	}

	[Fact]
	public void Create_AllSlotsStartEmpty()
	{
		var plan = WeeklyPlanBuilder.A().Build();

		plan.Slots.Should().OnlyContain(s => s.ContentType == SlotContentType.Empty);
	}

	[Fact]
	public void SwapSlots_RecipeWithFreetext_SwapsAllContent()
	{
		var plan = WeeklyPlanBuilder.A().Build();
		var pasta = Recipe("Pasta");
		var eatingOut = FreetextLabel.From("Eating out");
		plan.AssignRecipe(DayOfWeek.Monday, pasta);
		plan.SetFreetext(DayOfWeek.Tuesday, eatingOut);

		plan.SwapSlots(DayOfWeek.Monday, DayOfWeek.Tuesday);

		var monday = plan.Slots.First(slot => slot.Day == DayOfWeek.Monday);
		monday.ContentType.Should().Be(SlotContentType.Freetext);
		monday.FreetextLabel.Should().Be(eatingOut);
		monday.Recipe.Should().BeNull();

		var tuesday = plan.Slots.First(slot => slot.Day == DayOfWeek.Tuesday);
		tuesday.ContentType.Should().Be(SlotContentType.Recipe);
		tuesday.Recipe!.RecipeIdentifier.Should().Be(pasta.RecipeIdentifier);
		tuesday.FreetextLabel.Should().BeNull();
	}

	[Fact]
	public void SwapSlots_RecipeWithLeftover_SwapsAllContent()
	{
		var plan = WeeklyPlanBuilder.A().Build();
		var bolognese = Recipe("Bolognese");
		plan.AssignRecipe(DayOfWeek.Monday, bolognese);
		plan.SetLeftover(DayOfWeek.Wednesday, DayOfWeek.Monday, MealType.Dinner, bolognese.Title);

		plan.SwapSlots(DayOfWeek.Monday, DayOfWeek.Wednesday);

		var monday = plan.Slots.First(slot => slot.Day == DayOfWeek.Monday);
		monday.ContentType.Should().Be(SlotContentType.Leftover);
		monday.LeftoverSourceDay.Should().Be(DayOfWeek.Monday);

		var wednesday = plan.Slots.First(slot => slot.Day == DayOfWeek.Wednesday);
		wednesday.ContentType.Should().Be(SlotContentType.Recipe);
		wednesday.Recipe!.Title.Should().Be(bolognese.Title);
	}

	[Fact]
	public void SwapSlots_FreetextWithEmpty_MovesContent()
	{
		var plan = WeeklyPlanBuilder.A().Build();
		var pizzaOrder = FreetextLabel.From("Pizza order");
		plan.SetFreetext(DayOfWeek.Monday, pizzaOrder);

		plan.SwapSlots(DayOfWeek.Monday, DayOfWeek.Tuesday);

		plan.Slots.First(slot => slot.Day == DayOfWeek.Monday).IsEmpty.Should().BeTrue();
		plan.Slots.First(slot => slot.Day == DayOfWeek.Tuesday).FreetextLabel.Should().Be(pizzaOrder);
	}

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

	private static SlotRecipeReference Recipe(string title = "Pasta") =>
		SlotRecipeReference.From(SlotRecipeIdentifier.New(), SlotRecipeTitle.From(title));

	private static SlotRecipeReference Recipe(Guid id, string title) =>
		SlotRecipeReference.From(id, title);
}
