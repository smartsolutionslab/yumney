using FluentAssertions;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.Tests.WeeklyPlan;

public class WeeklyPlanIdentifierTests
{
    [Fact]
    public void New_CreatesNonEmptyGuid()
    {
        var id = WeeklyPlanIdentifier.New();

        id.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void From_ValidGuid_CreatesInstance()
    {
        var guid = Guid.NewGuid();
        var id = WeeklyPlanIdentifier.From(guid);

        id.Value.Should().Be(guid);
    }

    [Fact]
    public void From_EmptyGuid_ThrowsGuardException()
    {
        var act = () => WeeklyPlanIdentifier.From(Guid.Empty);

        act.Should().Throw<GuardException>();
    }
}
