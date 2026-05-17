using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.Tests.ShoppingList;

public class ServingsTests
{
	[Theory]
	[InlineData(1)]
	[InlineData(4)]
	[InlineData(100)]
	public void From_PositiveValue_IsAccepted(int value)
	{
		var servings = Servings.From(value);

		servings.Value.Should().Be(value);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void From_NonPositive_Throws(int value)
	{
		var act = () => Servings.From(value);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void FromNullable_Null_ReturnsNull()
	{
		Servings.FromNullable(null).Should().BeNull();
	}

	[Fact]
	public void FromNullable_PositiveValue_ReturnsServings()
	{
		var servings = Servings.FromNullable(2);

		servings.Should().NotBeNull();
		servings!.Value.Should().Be(2);
	}

	[Fact]
	public void FromNullable_ZeroValue_Throws()
	{
		// HasValue is true for 0 — the guard then rejects the non-positive
		// payload. FromNullable is "null-coalescing", not "zero-coalescing".
		var act = () => Servings.FromNullable(0);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void ImplicitConversion_ToInt_YieldsValue()
	{
		var servings = Servings.From(3);
		int raw = servings;

		raw.Should().Be(3);
	}

	[Fact]
	public void ToString_ReturnsInvariantNumericString()
	{
		var servings = Servings.From(4);

		servings.ToString().Should().Be("4");
	}

	[Fact]
	public void Equality_SameValue_AreEqual()
	{
		var a = Servings.From(2);
		var b = Servings.From(2);

		a.Should().Be(b);
		a.GetHashCode().Should().Be(b.GetHashCode());
	}
}
