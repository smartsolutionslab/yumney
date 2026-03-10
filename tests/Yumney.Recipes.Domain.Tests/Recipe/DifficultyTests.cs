using FluentAssertions;
using Xunit;
using Yumney.Recipes.Domain.Recipe;
using Yumney.Shared.Guards;

namespace Yumney.Recipes.Domain.Tests.Recipe;

public class DifficultyTests
{
    [Fact]
    public void Constructor_ValidValue_CreatesInstance()
    {
        var difficulty = new Difficulty("medium");

        difficulty.Value.Should().Be("medium");
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        var difficulty = new Difficulty("  easy  ");

        difficulty.Value.Should().Be("easy");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrWhitespace_ThrowsGuardException(string? value)
    {
        var act = () => new Difficulty(value!);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Constructor_ExceedsMaxLength_ThrowsGuardException()
    {
        var value = new string('a', 51);

        var act = () => new Difficulty(value);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Constructor_AtMaxLength_CreatesInstance()
    {
        var value = new string('a', 50);

        var difficulty = new Difficulty(value);

        difficulty.Value.Should().HaveLength(50);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var difficulty = new Difficulty("hard");

        difficulty.ToString().Should().Be("hard");
    }
}
