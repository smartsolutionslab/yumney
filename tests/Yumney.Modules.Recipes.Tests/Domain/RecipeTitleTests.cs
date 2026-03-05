using FluentAssertions;
using Xunit;
using Yumney.Modules.Recipes.Domain.Recipe;
using Yumney.Shared.Guards;

namespace Yumney.Modules.Recipes.Tests.Domain;

public class RecipeTitleTests
{
    [Fact]
    public void Constructor_WithValidTitle_ShouldCreateInstance()
    {
        var title = new RecipeTitle("Spaghetti Carbonara");

        title.Value.Should().Be("Spaghetti Carbonara");
    }

    [Fact]
    public void Constructor_WithWhitespace_ShouldTrim()
    {
        var title = new RecipeTitle("  Spaghetti Carbonara  ");

        title.Value.Should().Be("Spaghetti Carbonara");
    }

    [Fact]
    public void Constructor_WithEmptyString_ShouldThrow()
    {
        var act = () => new RecipeTitle("");

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Constructor_WithTooLongTitle_ShouldThrow()
    {
        string longTitle = new('x', 201);

        var act = () => new RecipeTitle(longTitle);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void ImplicitConversion_ShouldReturnValue()
    {
        var title = new RecipeTitle("Test");

        string result = title;

        result.Should().Be("Test");
    }
}
