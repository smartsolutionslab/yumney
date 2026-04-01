using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Recipe;

public class RecipeLanguageTests
{
    [Fact]
    public void From_ValidLanguage_CreatesInstance()
    {
        var language = RecipeLanguage.From("en");

        language.Value.Should().Be("en");
    }

    [Fact]
    public void From_TrimsWhitespace()
    {
        var language = RecipeLanguage.From("  en  ");

        language.Value.Should().Be("en");
    }

    [Fact]
    public void From_ConvertsToLowerCase()
    {
        var language = RecipeLanguage.From("EN");

        language.Value.Should().Be("en");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void From_NullOrWhitespace_ThrowsGuardException(string? value)
    {
        var act = () => RecipeLanguage.From(value!);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void From_ExceedsMaxLength_ThrowsGuardException()
    {
        var value = new string('a', 11);

        var act = () => RecipeLanguage.From(value);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void From_AtMaxLength_CreatesInstance()
    {
        var value = new string('a', 10);

        var language = RecipeLanguage.From(value);

        language.Value.Should().HaveLength(10);
    }

    [Fact]
    public void FromNullable_WithValue_ReturnsInstance()
    {
        var language = RecipeLanguage.FromNullable("en");

        language.Should().NotBeNull();
        language!.Value.Should().Be("en");
    }

    [Fact]
    public void FromNullable_WithNull_ReturnsNull()
    {
        var language = RecipeLanguage.FromNullable(null);

        language.Should().BeNull();
    }

    [Fact]
    public void FromNullable_WithEmpty_ReturnsNull()
    {
        var language = RecipeLanguage.FromNullable(string.Empty);

        language.Should().BeNull();
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var language1 = RecipeLanguage.From("en");
        var language2 = RecipeLanguage.From("en");

        language1.Should().Be(language2);
    }

    [Fact]
    public void ImplicitConversion_ReturnsValue()
    {
        var language = RecipeLanguage.From("en");

        string result = language;

        result.Should().Be("en");
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var language = RecipeLanguage.From("en");

        language.ToString().Should().Be("en");
    }
}
