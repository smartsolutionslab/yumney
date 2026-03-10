using FluentAssertions;
using Xunit;
using Yumney.Recipes.Domain.Recipe;

namespace Yumney.Recipes.Domain.Tests.Recipe;

public class StepTests
{
    [Fact]
    public void Create_ValidInput_SetsProperties()
    {
        var number = new StepNumber(1);
        var description = new StepDescription("Preheat oven to 180°C");

        var step = Step.Create(number, description);

        step.Id.Should().NotBeEmpty();
        step.Number.Should().Be(number);
        step.Description.Should().Be(description);
    }

    [Fact]
    public void Create_GeneratesUniqueIds()
    {
        var number = new StepNumber(1);
        var description = new StepDescription("Mix ingredients");

        var step1 = Step.Create(number, description);
        var step2 = Step.Create(number, description);

        step1.Id.Should().NotBe(step2.Id);
    }
}
