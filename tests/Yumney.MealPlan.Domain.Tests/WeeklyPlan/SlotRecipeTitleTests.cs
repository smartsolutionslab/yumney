using FluentAssertions;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.Tests.WeeklyPlan;

public class SlotRecipeTitleTests
{
	[Fact]
	public void From_ValidValue_CreatesInstance()
	{
		var title = SlotRecipeTitle.From("Pad Thai");

		title.Value.Should().Be("Pad Thai");
	}

	[Fact]
	public void From_ValueWithSurroundingWhitespace_TrimsValue()
	{
		var title = SlotRecipeTitle.From("  Carbonara  ");

		title.Value.Should().Be("Carbonara");
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void From_NullOrWhitespace_ThrowsGuardException(string? value)
	{
		var act = () => SlotRecipeTitle.From(value!);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void From_ExceedsMaxLength_ThrowsGuardException()
	{
		var act = () => SlotRecipeTitle.From(new string('x', SlotRecipeTitle.MaxLength + 1));

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void From_AtMaxLength_Succeeds()
	{
		var title = SlotRecipeTitle.From(new string('x', SlotRecipeTitle.MaxLength));

		title.Value.Should().HaveLength(SlotRecipeTitle.MaxLength);
	}

	[Fact]
	public void ImplicitConversion_ReturnsValueString()
	{
		string value = SlotRecipeTitle.From("Risotto");

		value.Should().Be("Risotto");
	}

	[Fact]
	public void Equality_SameValue_AreEqual()
	{
		var first = SlotRecipeTitle.From("Same");
		var second = SlotRecipeTitle.From("Same");

		first.Should().Be(second);
	}
}
