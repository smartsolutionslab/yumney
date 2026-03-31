using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Recipe;

public class ServingsTests
{
    [Fact]
    public void Constructor_PositiveValue_CreatesInstance()
    {
        var servings = Servings.From(4);

        servings.Value.Should().Be(4);
    }

    [Fact]
    public void Constructor_MinimumValue_CreatesInstance()
    {
        var servings = Servings.From(1);

        servings.Value.Should().Be(1);
    }

    [Fact]
    public void Constructor_Zero_ThrowsGuardException()
    {
        var act = () => Servings.From(0);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Constructor_NegativeValue_ThrowsGuardException()
    {
        var act = () => Servings.From(-1);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void ToString_ReturnsStringValue()
    {
        var servings = Servings.From(4);

        servings.ToString().Should().Be("4");
    }

    [Fact]
    public void FromNullable_Null_ReturnsNull()
    {
        Servings.FromNullable(null).Should().BeNull();
    }

    [Fact]
    public void FromNullable_ValidValue_ReturnsInstance()
    {
        var result = Servings.FromNullable(4);

        result.Should().NotBeNull();
        result!.Value.Should().Be(4);
    }
}
