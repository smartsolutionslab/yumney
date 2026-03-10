using FluentAssertions;
using Xunit;
using Yumney.Recipes.Domain.Recipe;
using Yumney.Shared.Guards;

namespace Yumney.Recipes.Domain.Tests.Recipe;

public class ServingsTests
{
    [Fact]
    public void Constructor_PositiveValue_CreatesInstance()
    {
        var servings = new Servings(4);

        servings.Value.Should().Be(4);
    }

    [Fact]
    public void Constructor_MinimumValue_CreatesInstance()
    {
        var servings = new Servings(1);

        servings.Value.Should().Be(1);
    }

    [Fact]
    public void Constructor_Zero_ThrowsGuardException()
    {
        var act = () => new Servings(0);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Constructor_NegativeValue_ThrowsGuardException()
    {
        var act = () => new Servings(-1);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void ToString_ReturnsStringValue()
    {
        var servings = new Servings(4);

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
