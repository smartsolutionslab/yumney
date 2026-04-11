using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Common;

public class QuantityRounderTests
{
    [Theory]
    [InlineData(5.3, 6)]
    [InlineData(1.1, 2)]
    [InlineData(0.5, 1)]
    [InlineData(3.9, 4)]
    public void RoundUp_FractionalQuantity_RoundsToNextInteger(decimal input, decimal expected)
    {
        var result = QuantityRounder.RoundUp(input, "L");

        result.DisplayQuantity.Should().Be(expected);
        result.ExactQuantity.Should().Be(input);
        result.WasRounded.Should().BeTrue();
    }

    [Theory]
    [InlineData(2)]
    [InlineData(1)]
    [InlineData(6)]
    public void RoundUp_WholeNumber_NoRounding(decimal input)
    {
        var result = QuantityRounder.RoundUp(input, "L");

        result.DisplayQuantity.Should().Be(input);
        result.ExactQuantity.Should().Be(input);
        result.WasRounded.Should().BeFalse();
    }

    [Fact]
    public void RoundUp_Zero_ReturnsZero()
    {
        var result = QuantityRounder.RoundUp(0, "pc");

        result.DisplayQuantity.Should().Be(0);
        result.WasRounded.Should().BeFalse();
    }

    [Fact]
    public void RoundUp_Negative_ReturnsAsIs()
    {
        var result = QuantityRounder.RoundUp(-1, "L");

        result.DisplayQuantity.Should().Be(-1);
        result.WasRounded.Should().BeFalse();
    }

    [Fact]
    public void RoundUp_NullUnit_StillRounds()
    {
        var result = QuantityRounder.RoundUp(2.5m, null);

        result.DisplayQuantity.Should().Be(3);
        result.WasRounded.Should().BeTrue();
    }
}
