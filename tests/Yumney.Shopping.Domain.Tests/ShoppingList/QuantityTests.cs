using FluentAssertions;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.Tests.ShoppingList;

public class QuantityTests
{
    [Fact]
    public void Of_WithAmountAndUnit_CreatesInstance()
    {
        var quantity = Quantity.Of(Amount.From(500), Unit.From("g"));

        quantity.Amount.Value.Should().Be(500);
        quantity.Unit!.Value.Should().Be("g");
    }

    [Fact]
    public void Of_WithAmountOnly_CreatesInstanceWithNullUnit()
    {
        var quantity = Quantity.Of(Amount.From(3), null);

        quantity.Amount.Value.Should().Be(3);
        quantity.Unit.Should().BeNull();
    }

    [Fact]
    public void FromNullable_WithAmount_CreatesInstance()
    {
        var quantity = Quantity.FromNullable(Amount.From(1), Unit.From("L"));

        quantity.Should().NotBeNull();
        quantity!.Amount.Value.Should().Be(1);
    }

    [Fact]
    public void FromNullable_NullAmount_ReturnsNull()
    {
        var quantity = Quantity.FromNullable(null, Unit.From("L"));

        quantity.Should().BeNull();
    }

    [Fact]
    public void ToString_WithUnit_ReturnsFormattedString()
    {
        var quantity = Quantity.Of(Amount.From(500), Unit.From("g"));

        quantity.ToString().Should().Be("500 g");
    }

    [Fact]
    public void ToString_WithoutUnit_ReturnsAmountOnly()
    {
        var quantity = Quantity.Of(Amount.From(3), null);

        quantity.ToString().Should().Be("3");
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = Quantity.Of(Amount.From(500), Unit.From("g"));
        var b = Quantity.Of(Amount.From(500), Unit.From("g"));

        a.Should().Be(b);
    }

    [Fact]
    public void Equality_DifferentUnit_AreNotEqual()
    {
        var a = Quantity.Of(Amount.From(500), Unit.From("g"));
        var b = Quantity.Of(Amount.From(500), Unit.From("ml"));

        a.Should().NotBe(b);
    }
}
