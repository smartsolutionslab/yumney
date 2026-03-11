using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Recipe;

public class IngredientNameTests
{
    [Fact]
    public void Constructor_ValidName_CreatesInstance()
    {
        var name = new IngredientName("Spaghetti");

        name.Value.Should().Be("Spaghetti");
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        var name = new IngredientName("  Flour  ");

        name.Value.Should().Be("Flour");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrWhitespace_ThrowsGuardException(string? value)
    {
        var act = () => new IngredientName(value!);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Constructor_ExceedsMaxLength_ThrowsGuardException()
    {
        var value = new string('a', 201);

        var act = () => new IngredientName(value);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Constructor_AtMaxLength_CreatesInstance()
    {
        var value = new string('a', 200);

        var name = new IngredientName(value);

        name.Value.Should().HaveLength(200);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var name = new IngredientName("Sugar");

        name.ToString().Should().Be("Sugar");
    }
}
