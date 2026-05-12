using FluentAssertions;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.Tests.WeeklyPlan;

public class SlotServingsTests
{
	[Fact]
	public void From_PositiveValue_CreatesInstance()
	{
		var servings = SlotServings.From(4);

		servings.Value.Should().Be(4);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(int.MinValue)]
	public void From_NonPositiveValue_ThrowsGuardException(int value)
	{
		var act = () => SlotServings.From(value);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void FromNullable_NullValue_ReturnsNull()
	{
		var servings = SlotServings.FromNullable(null);

		servings.Should().BeNull();
	}

	[Fact]
	public void FromNullable_PositiveValue_CreatesInstance()
	{
		var servings = SlotServings.FromNullable(8);

		servings!.Value.Should().Be(8);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-3)]
	public void FromNullable_NonPositiveValue_ThrowsGuardException(int value)
	{
		var act = () => SlotServings.FromNullable(value);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void Default_ReturnsConfiguredDefault()
	{
		var servings = SlotServings.Default();

		servings.Value.Should().Be(SlotServings.DefaultValue);
	}

	[Fact]
	public void ImplicitConversion_ReturnsValueInt()
	{
		int value = SlotServings.From(6);

		value.Should().Be(6);
	}

	[Fact]
	public void ToString_ReturnsValueAsInvariantString()
	{
		var servings = SlotServings.From(12);

		servings.ToString().Should().Be("12");
	}

	[Fact]
	public void Equality_SameValue_AreEqual()
	{
		var first = SlotServings.From(4);
		var second = SlotServings.From(4);

		first.Should().Be(second);
	}

	[Fact]
	public void Equality_DifferentValue_AreNotEqual()
	{
		var first = SlotServings.From(4);
		var second = SlotServings.From(5);

		first.Should().NotBe(second);
	}
}
