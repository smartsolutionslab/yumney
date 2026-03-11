using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Recipe;

public class CookingTimeTests
{
    [Fact]
    public void Constructor_PositiveValue_CreatesInstance()
    {
        var cookTime = new CookingTime(45);

        cookTime.Value.Should().Be(45);
    }

    [Fact]
    public void Constructor_Zero_CreatesInstance()
    {
        var cookTime = new CookingTime(0);

        cookTime.Value.Should().Be(0);
    }

    [Fact]
    public void Constructor_NegativeValue_ThrowsGuardException()
    {
        var act = () => new CookingTime(-1);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void ToString_ReturnsStringValue()
    {
        var cookTime = new CookingTime(20);

        cookTime.ToString().Should().Be("20");
    }

    [Fact]
    public void FromNullable_Null_ReturnsNull()
    {
        CookingTime.FromNullable(null).Should().BeNull();
    }

    [Fact]
    public void FromNullable_ValidValue_ReturnsInstance()
    {
        var result = CookingTime.FromNullable(45);

        result.Should().NotBeNull();
        result!.Value.Should().Be(45);
    }
}
