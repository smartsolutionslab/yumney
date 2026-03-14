using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.Tests.ShoppingList;

public class AmountTests
{
    [Fact]
    public void Constructor_PositiveValue_CreatesInstance()
    {
        var amount = new Amount(500);

        amount.Value.Should().Be(500);
    }

    [Fact]
    public void Constructor_Zero_CreatesInstance()
    {
        var amount = new Amount(0);

        amount.Value.Should().Be(0);
    }

    [Fact]
    public void Constructor_NegativeValue_ThrowsGuardException()
    {
        var act = () => new Amount(-1);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void FromNullable_WithValue_CreatesInstance()
    {
        var amount = Amount.FromNullable(100m);

        amount.Should().NotBeNull();
        amount!.Value.Should().Be(100);
    }

    [Fact]
    public void FromNullable_Null_ReturnsNull()
    {
        var amount = Amount.FromNullable(null);

        amount.Should().BeNull();
    }

    [Fact]
    public void ToString_ReturnsFormattedValue()
    {
        var amount = new Amount(1.5m);

        amount.ToString().Should().Be("1.5");
    }
}
