using FluentAssertions;
using Xunit;
using Yumney.Recipes.Domain.Recipe;
using Yumney.Shared.Guards;

namespace Yumney.Recipes.Domain.Tests.Recipe;

public class RecipeDescriptionTests
{
    [Fact]
    public void Constructor_ValidDescription_CreatesInstance()
    {
        var description = new RecipeDescription("A classic Italian pasta dish");

        description.Value.Should().Be("A classic Italian pasta dish");
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        var description = new RecipeDescription("  Some description  ");

        description.Value.Should().Be("Some description");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrWhitespace_ThrowsGuardException(string? value)
    {
        var act = () => new RecipeDescription(value!);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Constructor_ExceedsMaxLength_ThrowsGuardException()
    {
        var value = new string('a', 2001);

        var act = () => new RecipeDescription(value);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Constructor_AtMaxLength_CreatesInstance()
    {
        var value = new string('a', 2000);

        var description = new RecipeDescription(value);

        description.Value.Should().HaveLength(2000);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var description = new RecipeDescription("Test");

        description.ToString().Should().Be("Test");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FromNullable_NullOrWhitespace_ReturnsNull(string? value)
    {
        RecipeDescription.FromNullable(value).Should().BeNull();
    }

    [Fact]
    public void FromNullable_ValidValue_ReturnsInstance()
    {
        var result = RecipeDescription.FromNullable("A tasty recipe");

        result.Should().NotBeNull();
        result!.Value.Should().Be("A tasty recipe");
    }
}
