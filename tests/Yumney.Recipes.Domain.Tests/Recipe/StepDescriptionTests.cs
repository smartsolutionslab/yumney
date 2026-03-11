using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Recipe;

public class StepDescriptionTests
{
    [Fact]
    public void Constructor_ValidDescription_CreatesInstance()
    {
        var description = new StepDescription("Cook pasta until al dente");

        description.Value.Should().Be("Cook pasta until al dente");
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        var description = new StepDescription("  Mix ingredients  ");

        description.Value.Should().Be("Mix ingredients");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrWhitespace_ThrowsGuardException(string? value)
    {
        var act = () => new StepDescription(value!);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Constructor_ExceedsMaxLength_ThrowsGuardException()
    {
        var value = new string('a', 2001);

        var act = () => new StepDescription(value);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Constructor_AtMaxLength_CreatesInstance()
    {
        var value = new string('a', 2000);

        var description = new StepDescription(value);

        description.Value.Should().HaveLength(2000);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var description = new StepDescription("Boil water");

        description.ToString().Should().Be("Boil water");
    }
}
