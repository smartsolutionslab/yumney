using FluentAssertions;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using Xunit;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.Tests.WeeklyPlan;

public class MealSlotTests
{
	[Fact]
	public void Empty_FactoryProducesEmptyContent()
	{
		var slot = MealSlot.Empty(DayOfWeek.Monday, MealType.Dinner, SlotServings.Default());

		slot.Day.Should().Be(DayOfWeek.Monday);
		slot.MealType.Should().Be(MealType.Dinner);
		slot.ContentType.Should().Be(SlotContentType.Empty);
		slot.Recipe.Should().BeNull();
		slot.FreetextLabel.Should().BeNull();
		slot.LeftoverLabel.Should().BeNull();
		slot.LeftoverSourceDay.Should().BeNull();
		slot.LeftoverSourceMealType.Should().BeNull();
		slot.State.Should().Be(MealState.Planned);
	}

	[Fact]
	public void IsEmpty_EmptyContent_ReturnsTrue()
	{
		var slot = MealSlot.Empty(DayOfWeek.Tuesday, MealType.Lunch, SlotServings.Default());

		slot.IsEmpty.Should().BeTrue();
	}

	[Fact]
	public void IsEmpty_RecipeContent_ReturnsFalse()
	{
		var slot = new MealSlot(
			DayOfWeek.Wednesday,
			MealType.Dinner,
			SlotContentType.Recipe,
			SlotRecipeReference.From(Guid.NewGuid(), "Pad Thai"),
			SlotServings.From(2),
			null,
			null,
			null,
			null,
			MealState.Planned);

		slot.IsEmpty.Should().BeFalse();
	}

	[Fact]
	public void Empty_PreservesProvidedServings()
	{
		var servings = SlotServings.From(6);

		var slot = MealSlot.Empty(DayOfWeek.Friday, MealType.Lunch, servings);

		slot.Servings.Should().Be(servings);
	}

	[Fact]
	public void Equality_SameValues_AreEqual()
	{
		var first = MealSlot.Empty(DayOfWeek.Monday, MealType.Dinner, SlotServings.Default());
		var second = MealSlot.Empty(DayOfWeek.Monday, MealType.Dinner, SlotServings.Default());

		first.Should().Be(second);
	}

	[Fact]
	public void Equality_DifferentDay_AreNotEqual()
	{
		var first = MealSlot.Empty(DayOfWeek.Monday, MealType.Dinner, SlotServings.Default());
		var second = MealSlot.Empty(DayOfWeek.Tuesday, MealType.Dinner, SlotServings.Default());

		first.Should().NotBe(second);
	}
}
