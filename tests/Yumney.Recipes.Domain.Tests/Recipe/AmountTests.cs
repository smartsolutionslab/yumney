using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Recipe;

public class AmountTests
{
    [Fact]
    public void Constructor_PositiveValue_CreatesInstance()
    {
        var amount = Amount.From(2.5m);

        amount.Value.Should().Be(2.5m);
    }

    [Fact]
    public void Constructor_Zero_CreatesInstance()
    {
        var amount = Amount.From(0m);

        amount.Value.Should().Be(0m);
    }

    [Fact]
    public void Constructor_NegativeValue_ThrowsGuardException()
    {
        var act = () => Amount.From(-1m);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void ToString_ReturnsStringValue()
    {
        var amount = Amount.From(3.14m);

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
