using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Recipe;

public class StepNumberTests
{
    [Fact]
    public void Constructor_PositiveValue_CreatesInstance()
    {
        var stepNumber = new StepNumber(1);

        stepNumber.Value.Should().Be(1);
    }

    [Fact]
    public void Constructor_Zero_ThrowsGuardException()
    {
        var act = () => new StepNumber(0);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Constructor_NegativeValue_ThrowsGuardException()
    {
        var act = () => new StepNumber(-1);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void ToString_ReturnsStringValue()
    {
        var stepNumber = new StepNumber(3);

        stepNumber.ToString().Should().Be("3");
    }
}
