using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Recipe;

public class RecipeDescriptionTests
{
    [Fact]
    public void Constructor_ValidDescription_CreatesInstance()
    {
        var description = RecipeDescription.From("A classic Italian pasta dish");

        description.Value.Should().Be("A classic Italian pasta dish");
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        var description = RecipeDescription.From("  Some description  ");

        description.Value.Should().Be("Some description");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrWhitespace_ThrowsGuardException(string? value)
    {
        var act = () => RecipeDescription.From(value!);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Constructor_ExceedsMaxLength_ThrowsGuardException()
    {
        var value = new string('a', 2001);

        var act = () => RecipeDescription.From(value);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Constructor_AtMaxLength_CreatesInstance()
    {
        var value = new string('a', 2000);

        var description = RecipeDescription.From(value);

        description.Value.Should().HaveLength(2000);
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
