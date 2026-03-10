using FluentAssertions;
using Xunit;
using Yumney.Recipes.Domain.Recipe;
using Yumney.Shared.Guards;

namespace Yumney.Recipes.Domain.Tests.Recipe;

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
}
