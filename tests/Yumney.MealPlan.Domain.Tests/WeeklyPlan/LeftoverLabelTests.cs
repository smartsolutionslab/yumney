using FluentAssertions;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.Tests.WeeklyPlan;

public class LeftoverLabelTests
{
	[Fact]
	public void From_ValidValue_CreatesInstance()
	{
		var label = LeftoverLabel.From("Leftovers: Lasagna");

		label.Value.Should().Be("Leftovers: Lasagna");
	}

	[Fact]
	public void From_ValueWithSurroundingWhitespace_TrimsValue()
	{
		var label = LeftoverLabel.From("  Leftovers  ");

		label.Value.Should().Be("Leftovers");
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void From_NullOrWhitespace_ThrowsGuardException(string? value)
	{
		var act = () => LeftoverLabel.From(value!);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void From_ExceedsMaxLength_ThrowsGuardException()
	{
		var act = () => LeftoverLabel.From(new string('x', 201));

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void ForRecipe_PrefixesWithLeftoversLabel()
	{
		var label = LeftoverLabel.ForRecipe("Spaghetti Bolognese");

		label.Value.Should().Be("Leftovers: Spaghetti Bolognese");
	}

	[Fact]
	public void ImplicitConversion_ReturnsValueString()
	{
		string value = LeftoverLabel.From("Cold pizza");

		value.Should().Be("Cold pizza");
	}

	[Fact]
	public void ToString_ReturnsValue()
	{
		var label = LeftoverLabel.From("Risotto");

		label.ToString().Should().Be("Risotto");
	}
}
