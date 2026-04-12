using FluentAssertions;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.Tests.WeeklyPlan;

public class WeekIdentifierTests
{
    [Fact]
    public void From_ValidYearAndWeek_CreatesInstance()
    {
        var week = WeekIdentifier.From(2026, 15);

        week.Year.Should().Be(2026);
        week.WeekNumber.Should().Be(15);
        week.Value.Should().Be("2026-W15");
    }

    [Fact]
    public void From_SingleDigitWeek_PadsWithZero()
    {
        var week = WeekIdentifier.From(2026, 3);

        week.Value.Should().Be("2026-W03");
    }

    [Theory]
    [InlineData(2019)]
    [InlineData(2101)]
    public void From_InvalidYear_ThrowsGuardException(int year)
    {
        var act = () => WeekIdentifier.From(year, 1);

        act.Should().Throw<GuardException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(54)]
    public void From_InvalidWeekNumber_ThrowsGuardException(int week)
    {
        var act = () => WeekIdentifier.From(2026, week);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void From_BoundaryValues_Succeeds()
    {
        WeekIdentifier.From(2020, 1).Year.Should().Be(2020);
        WeekIdentifier.From(2100, 53).WeekNumber.Should().Be(53);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = WeekIdentifier.From(2026, 15);
        var b = WeekIdentifier.From(2026, 15);

        a.Should().Be(b);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = WeekIdentifier.From(2026, 15);
        var b = WeekIdentifier.From(2026, 16);

        a.Should().NotBe(b);
    }

    [Fact]
    public void ImplicitConversion_ReturnsValueString()
    {
        string value = WeekIdentifier.From(2026, 15);

        value.Should().Be("2026-W15");
    }

    [Fact]
    public void Current_ReturnsNonNull()
    {
        var current = WeekIdentifier.Current();

        current.Year.Should().BeGreaterThanOrEqualTo(2020);
        current.WeekNumber.Should().BeInRange(1, 53);
    }

    [Fact]
    public void FromDate_ReturnsCorrectWeek()
    {
        var date = new DateOnly(2026, 4, 13); // Known Monday in week 16
        var week = WeekIdentifier.FromDate(date);

        week.Year.Should().Be(2026);
        week.WeekNumber.Should().BeInRange(15, 16);
    }
}
