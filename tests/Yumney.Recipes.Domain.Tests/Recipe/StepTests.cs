using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Recipe;

public class StepTests
{
    [Fact]
    public void Create_ValidInput_SetsProperties()
    {
        var number = StepNumber.From(1);
        var description = StepDescription.From("Preheat oven to 180°C");

        var step = Step.Create(number, description);

        step.Id.Should().NotBeNull();
        step.Number.Should().Be(number);
        step.Description.Should().Be(description);
    }

    [Fact]
    public void Create_GeneratesUniqueIds()
    {
        var number = StepNumber.From(1);
        var description = StepDescription.From("Mix ingredients");

        var step1 = Step.Create(number, description);
        var step2 = Step.Create(number, description);

        step1.Id.Should().NotBe(step2.Id);
    }
}
