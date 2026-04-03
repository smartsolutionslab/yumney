using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Recipe;

public class DifficultyTests
{
    [Fact]
    public void Constructor_ValidValue_CreatesInstance()
    {
        var difficulty = Difficulty.From("medium");

        difficulty.Value.Should().Be("medium");
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        var difficulty = Difficulty.From("  easy  ");

        difficulty.Value.Should().Be("easy");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrWhitespace_ThrowsGuardException(string? value)
    {
        var act = () => Difficulty.From(value!);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Constructor_ExceedsMaxLength_ThrowsGuardException()
    {
        var value = new string('a', 51);

        var act = () => Difficulty.From(value);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Constructor_AtMaxLength_CreatesInstance()
    {
        var value = new string('a', 50);

        var difficulty = Difficulty.From(value);

        difficulty.Value.Should().HaveLength(50);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FromNullable_NullOrWhitespace_ReturnsNull(string? value)
    {
        Difficulty.FromNullable(value).Should().BeNull();
    }

    [Fact]
    public void FromNullable_ValidValue_ReturnsInstance()
    {
        var result = Difficulty.FromNullable("easy");

        result.Should().NotBeNull();
        result!.Value.Should().Be("easy");
    }
}
