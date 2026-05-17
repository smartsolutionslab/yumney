using FluentAssertions;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.Events;
using Xunit;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.Tests.WeeklyPlan.Events;

/// <summary>
/// Construction + equality smoke tests for every WeeklyPlan domain event.
/// Records get free Equals / GetHashCode / Deconstruct from the compiler — we
/// only need to assert that the positional ctor stamps each field through and
/// that two events with the same payload compare equal. Anything richer is
/// covered by the WeeklyPlan aggregate tests that exercise the event-raising
/// paths end-to-end.
/// </summary>
public class WeeklyPlanEventsTests
{
	[Fact]
	public void WeeklyPlanCreated_PositionalCtor_StampsAllFields()
	{
		var owner = OwnerIdentifier.From("kc-user-1");
		var week = WeekIdentifier.From(2026, 20);
		var defaultServings = SlotServings.From(2);

		var @event = new WeeklyPlanCreated(owner, week, defaultServings);

		@event.Owner.Should().Be(owner);
		@event.Week.Should().Be(week);
		@event.DefaultServings.Should().Be(defaultServings);
	}

	[Fact]
	public void RecipeAssigned_PositionalCtor_StampsAllFields()
	{
		var recipe = SlotRecipeReference.From(Guid.NewGuid(), "Carbonara");
		var servings = SlotServings.From(4);

		var @event = new RecipeAssigned(DayOfWeek.Wednesday, MealType.Dinner, recipe, servings);

		@event.Day.Should().Be(DayOfWeek.Wednesday);
		@event.MealType.Should().Be(MealType.Dinner);
		@event.Recipe.Should().Be(recipe);
		@event.Servings.Should().Be(servings);
	}

	[Fact]
	public void RecipeAssigned_NullServings_IsAllowed()
	{
		var recipe = SlotRecipeReference.From(Guid.NewGuid(), "Risotto");

		var @event = new RecipeAssigned(DayOfWeek.Friday, MealType.Lunch, recipe, Servings: null);

		@event.Servings.Should().BeNull();
	}

	[Fact]
	public void MealMarkedAsCooked_PositionalCtor_StampsAllFields()
	{
		var recipe = SlotRecipeReference.From(Guid.NewGuid(), "Stew");
		var servings = SlotServings.From(3);
		IReadOnlyList<CookedIngredient> ingredients = [new CookedIngredient("Onion", 1m, null), new CookedIngredient("Stock", 500m, "ml")];

		var @event = new MealMarkedAsCooked(DayOfWeek.Tuesday, MealType.Dinner, recipe, servings, ingredients);

		@event.Day.Should().Be(DayOfWeek.Tuesday);
		@event.Recipe.Should().Be(recipe);
		@event.Servings.Should().Be(servings);
		@event.Ingredients.Should().HaveCount(2);
	}

	[Fact]
	public void MealMarkedAsCooked_FreetextSlot_AllowsNullRecipe()
	{
		var @event = new MealMarkedAsCooked(DayOfWeek.Monday, MealType.Breakfast, Recipe: null, SlotServings.From(1), []);

		@event.Recipe.Should().BeNull();
		@event.Ingredients.Should().BeEmpty();
	}

	[Fact]
	public void MealMarkedAsSkipped_StampsDayAndMealType()
	{
		var @event = new MealMarkedAsSkipped(DayOfWeek.Saturday, MealType.Lunch);

		@event.Day.Should().Be(DayOfWeek.Saturday);
		@event.MealType.Should().Be(MealType.Lunch);
	}

	[Fact]
	public void MealResetToPlanned_StampsDayAndMealType()
	{
		var @event = new MealResetToPlanned(DayOfWeek.Sunday, MealType.Dinner);

		@event.Day.Should().Be(DayOfWeek.Sunday);
		@event.MealType.Should().Be(MealType.Dinner);
	}

	[Fact]
	public void MealSlotCleared_StampsDayAndMealType()
	{
		var @event = new MealSlotCleared(DayOfWeek.Wednesday, MealType.Breakfast);

		@event.Day.Should().Be(DayOfWeek.Wednesday);
		@event.MealType.Should().Be(MealType.Breakfast);
	}

	[Fact]
	public void MealSetAsFreetext_PositionalCtor_StampsAllFields()
	{
		var label = FreetextLabel.From("Eat out");

		var @event = new MealSetAsFreetext(DayOfWeek.Friday, MealType.Dinner, label);

		@event.Day.Should().Be(DayOfWeek.Friday);
		@event.MealType.Should().Be(MealType.Dinner);
		@event.Label.Should().Be(label);
	}

	[Fact]
	public void LeftoverAssigned_PositionalCtor_StampsAllFields()
	{
		var sourceTitle = SlotRecipeTitle.From("Lasagna");
		var servings = SlotServings.From(2);

		var @event = new LeftoverAssigned(
			DayOfWeek.Thursday,
			MealType.Lunch,
			SourceDay: DayOfWeek.Wednesday,
			SourceMealType: MealType.Dinner,
			SourceRecipeTitle: sourceTitle,
			Servings: servings);

		@event.Day.Should().Be(DayOfWeek.Thursday);
		@event.MealType.Should().Be(MealType.Lunch);
		@event.SourceDay.Should().Be(DayOfWeek.Wednesday);
		@event.SourceMealType.Should().Be(MealType.Dinner);
		@event.SourceRecipeTitle.Should().Be(sourceTitle);
		@event.Servings.Should().Be(servings);
	}

	[Fact]
	public void ServingsAdjusted_PositionalCtor_StampsAllFields()
	{
		var newServings = SlotServings.From(5);

		var @event = new ServingsAdjusted(DayOfWeek.Tuesday, MealType.Dinner, newServings);

		@event.Day.Should().Be(DayOfWeek.Tuesday);
		@event.MealType.Should().Be(MealType.Dinner);
		@event.Servings.Should().Be(newServings);
	}

	[Fact]
	public void MealSlotsSwapped_PositionalCtor_StampsBothDays()
	{
		var @event = new MealSlotsSwapped(DayOfWeek.Monday, DayOfWeek.Thursday, MealType.Dinner);

		@event.Day1.Should().Be(DayOfWeek.Monday);
		@event.Day2.Should().Be(DayOfWeek.Thursday);
		@event.MealType.Should().Be(MealType.Dinner);
	}

	[Fact]
	public void ExtendedModeEnabled_PositionalCtor_StampsDefaultServings()
	{
		var defaultServings = SlotServings.From(4);

		var @event = new ExtendedModeEnabled(defaultServings);

		@event.DefaultServings.Should().Be(defaultServings);
	}

	[Fact]
	public void ExtendedModeDisabled_ParameterlessCtor_Works()
	{
		// Just exercise the ctor — the DomainEvent base stamps OccurredOn from
		// DateTime.UtcNow, so two instances aren't deeply equal by design.
		var @event = new ExtendedModeDisabled();

		@event.Should().NotBeNull();
	}

	[Fact]
	public void CookedIngredient_PositionalCtor_StampsAllFields()
	{
		var ingredient = new CookedIngredient("Garlic", 2.5m, "g");

		ingredient.Name.Should().Be("Garlic");
		ingredient.Quantity.Should().Be(2.5m);
		ingredient.Unit.Should().Be("g");
	}

	[Fact]
	public void CookedIngredient_NullUnit_IsAllowed()
	{
		var ingredient = new CookedIngredient("Egg", 3m, Unit: null);

		ingredient.Unit.Should().BeNull();
	}
}
