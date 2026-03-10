using FluentAssertions;
using Xunit;
using Yumney.Recipes.Domain.Recipe;
using Yumney.Shared.Guards;

namespace Yumney.Recipes.Domain.Tests.Recipe;

public class AmountTests
{
    [Fact]
    public void Constructor_PositiveValue_CreatesInstance()
    {
        var amount = new Amount(2.5m);

        amount.Value.Should().Be(2.5m);
    }

    [Fact]
    public void Constructor_Zero_CreatesInstance()
    {
        var amount = new Amount(0m);

        amount.Value.Should().Be(0m);
    }

    [Fact]
    public void Constructor_NegativeValue_ThrowsGuardException()
    {
        var act = () => new Amount(-1m);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void ToString_ReturnsStringValue()
    {
        var amount = new Amount(3.14m);

        amount.ToString().Should().Be("3.14");
    }

    [Fact]
    public void FromNullable_Null_ReturnsNull()
    {
        Amount.FromNullable(null).Should().BeNull();
    }

    [Fact]
    public void FromNullable_ValidValue_ReturnsInstance()
    {
        var result = Amount.FromNullable(2.5m);

        result.Should().NotBeNull();
        result!.Value.Should().Be(2.5m);
    }
}
