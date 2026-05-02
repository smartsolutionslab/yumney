using FluentAssertions;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.Tests.WeeklyPlan;

public class FreetextLabelTests
{
	[Fact]
	public void From_ValidValue_CreatesInstance()
	{
		var label = FreetextLabel.From("Leftover pizza");

		label.Value.Should().Be("Leftover pizza");
	}

	[Fact]
	public void From_ValueWithSurroundingWhitespace_TrimsValue()
	{
		var label = FreetextLabel.From("  Pizza night  ");

		label.Value.Should().Be("Pizza night");
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void From_NullOrWhitespace_ThrowsGuardException(string? value)
	{
		var act = () => FreetextLabel.From(value!);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void From_ExceedsMaxLength_ThrowsGuardException()
	{
		var act = () => FreetextLabel.From(new string('x', 201));

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void From_AtMaxLength_Succeeds()
	{
		var label = FreetextLabel.From(new string('x', 200));

		label.Value.Should().HaveLength(200);
	}

	[Fact]
	public void ImplicitConversion_ReturnsValueString()
	{
		string value = FreetextLabel.From("Snack");

		value.Should().Be("Snack");
	}

	[Fact]
	public void ToString_ReturnsValue()
	{
		var label = FreetextLabel.From("Brunch");

		label.ToString().Should().Be("Brunch");
	}

	[Fact]
	public void Equality_SameValue_AreEqual()
	{
		var first = FreetextLabel.From("Same");
		var second = FreetextLabel.From("Same");

		first.Should().Be(second);
	}
}
