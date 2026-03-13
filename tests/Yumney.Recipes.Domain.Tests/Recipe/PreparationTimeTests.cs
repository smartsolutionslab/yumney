using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Recipe;

public class PreparationTimeTests
{
    [Fact]
    public void Constructor_PositiveValue_CreatesInstance()
    {
        var prepTime = new PreparationTime(30);

        prepTime.Value.Should().Be(30);
    }

    [Fact]
    public void Constructor_Zero_CreatesInstance()
    {
        var prepTime = new PreparationTime(0);

        prepTime.Value.Should().Be(0);
    }

    [Fact]
    public void Constructor_NegativeValue_ThrowsGuardException()
    {
        var act = () => new PreparationTime(-1);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void ToString_ReturnsStringValue()
    {
        var prepTime = new PreparationTime(15);

        prepTime.ToString().Should().Be("15");
    }

    [Fact]
    public void FromNullable_Null_ReturnsNull()
    {
        PreparationTime.FromNullable(null).Should().BeNull();
    }

    [Fact]
    public void FromNullable_ValidValue_ReturnsInstance()
    {
        var result = PreparationTime.FromNullable(30);

        result.Should().NotBeNull();
        result!.Value.Should().Be(30);
    }
}
