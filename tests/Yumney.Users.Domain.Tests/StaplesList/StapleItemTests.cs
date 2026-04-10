using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Users.Domain.StaplesList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Domain.Tests.StaplesList;

public class StapleItemTests
{
    [Theory]
    [InlineData("salt", "salt")]
    [InlineData("Olive Oil", "olive oil")]
    [InlineData("  PEPPER  ", "pepper")]
    public void From_ValidValue_NormalizesToLowerTrimmed(string input, string expected)
    {
        var item = StapleItem.From(input);

        item.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void From_NullOrWhitespace_ThrowsGuardException(string? value)
    {
        var act = () => StapleItem.From(value!);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Equality_SameNormalizedValue_AreEqual()
    {
        var i1 = StapleItem.From("Salt");
        var i2 = StapleItem.From("salt");

        i1.Should().Be(i2);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var i1 = StapleItem.From("salt");
        var i2 = StapleItem.From("pepper");

        i1.Should().NotBe(i2);
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        var item = StapleItem.From("butter");

        string value = item;

        value.Should().Be("butter");
    }
}
