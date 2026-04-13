using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Recipe;

public class DifficultyTests
{
    [Theory]
    [InlineData("Easy")]
    [InlineData("Medium")]
    [InlineData("Hard")]
    public void From_KnownValue_CreatesInstance(string value)
    {
        var difficulty = Difficulty.From(value);

        difficulty.Value.Should().Be(value);
    }

    [Theory]
    [InlineData("easy", "Easy")]
    [InlineData("MEDIUM", "Medium")]
    [InlineData("hArD", "Hard")]
    public void From_CaseInsensitive_ReturnsCanonicalCasing(string input, string expected)
    {
        var difficulty = Difficulty.From(input);

        difficulty.Value.Should().Be(expected);
    }

    [Fact]
    public void From_TrimsWhitespace()
    {
        var difficulty = Difficulty.From("  easy  ");

        difficulty.Value.Should().Be("Easy");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void From_NullOrWhitespace_ThrowsGuardException(string? value)
    {
        var act = () => Difficulty.From(value!);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void From_UnknownValue_ThrowsGuardException()
    {
        var act = () => Difficulty.From("expert");

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void From_ReturnsKnownInstance()
    {
        var difficulty = Difficulty.From("easy");

        difficulty.Should().BeSameAs(Difficulty.Easy);
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
        result!.Value.Should().Be("Easy");
    }

    [Fact]
    public void AllValues_ContainsAllKnownDifficulties()
    {
        Difficulty.AllValues.Should().BeEquivalentTo(["Easy", "Medium", "Hard"]);
    }
}
