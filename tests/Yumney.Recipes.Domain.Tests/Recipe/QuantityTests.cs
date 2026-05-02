using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Recipe;

public class QuantityTests
{
	[Fact]
	public void Of_AmountAndUnit_CreatesInstance()
	{
		var quantity = Quantity.Of(Amount.From(2.5m), Unit.Gram);

		quantity.Amount.Value.Should().Be(2.5m);
		quantity.Unit.Should().Be(Unit.Gram);
	}

	[Fact]
	public void Of_NullUnit_CreatesInstanceWithoutUnit()
	{
		var quantity = Quantity.Of(Amount.From(3m), null);

		quantity.Amount.Value.Should().Be(3m);
		quantity.Unit.Should().BeNull();
	}

	[Fact]
	public void FromNullable_NullAmount_ReturnsNull()
	{
		var quantity = Quantity.FromNullable(null, Unit.Gram);

		quantity.Should().BeNull();
	}

	[Fact]
	public void FromNullable_NonNullAmount_ReturnsInstance()
	{
		var quantity = Quantity.FromNullable(Amount.From(5m), Unit.Liter);

		quantity.Should().NotBeNull();
		quantity!.Amount.Value.Should().Be(5m);
		quantity.Unit.Should().Be(Unit.Liter);
	}

	[Fact]
	public void FromNullable_NonNullAmountAndNullUnit_ReturnsInstanceWithoutUnit()
	{
		var quantity = Quantity.FromNullable(Amount.From(1m), null);

		quantity.Should().NotBeNull();
		quantity!.Unit.Should().BeNull();
	}

	[Fact]
	public void ToString_WithUnit_IncludesBothAmountAndUnit()
	{
		var quantity = Quantity.Of(Amount.From(2m), Unit.Gram);

		quantity.ToString().Should().Contain("2").And.Contain("g");
	}

	[Fact]
	public void ToString_WithoutUnit_FormatsAmountOnly()
	{
		var quantity = Quantity.Of(Amount.From(3m), null);

		quantity.ToString().Should().Be("3");
	}

	[Fact]
	public void Equality_SameAmountAndUnit_AreEqual()
	{
		var first = Quantity.Of(Amount.From(2m), Unit.Gram);
		var second = Quantity.Of(Amount.From(2m), Unit.Gram);

		first.Should().Be(second);
	}

	[Fact]
	public void Equality_DifferentAmount_AreNotEqual()
	{
		var first = Quantity.Of(Amount.From(2m), Unit.Gram);
		var second = Quantity.Of(Amount.From(3m), Unit.Gram);

		first.Should().NotBe(second);
	}

	[Fact]
	public void Equality_DifferentUnit_AreNotEqual()
	{
		var first = Quantity.Of(Amount.From(2m), Unit.Gram);
		var second = Quantity.Of(Amount.From(2m), Unit.Kilogram);

		first.Should().NotBe(second);
	}
}
