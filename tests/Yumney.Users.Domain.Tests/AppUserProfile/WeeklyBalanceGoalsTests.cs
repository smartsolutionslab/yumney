using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Domain.Tests.AppUserProfile;

public class WeeklyBalanceGoalsTests
{
    [Fact]
    public void From_ValidValues_CreatesInstance()
    {
        var goals = WeeklyBalanceGoals.From(2, 3);

        goals.MinVeggieMeals.Should().Be(2);
        goals.MaxRedMeatMeals.Should().Be(3);
    }

    [Fact]
    public void From_NullValues_CreatesEmptyInstance()
    {
        var goals = WeeklyBalanceGoals.From(null, null);

        goals.MinVeggieMeals.Should().BeNull();
        goals.MaxRedMeatMeals.Should().BeNull();
        goals.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void From_PartialValues_CreatesInstance()
    {
        var goals = WeeklyBalanceGoals.From(3, null);

        goals.MinVeggieMeals.Should().Be(3);
        goals.MaxRedMeatMeals.Should().BeNull();
        goals.IsEmpty.Should().BeFalse();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(8)]
    public void From_MinVeggieOutOfRange_ThrowsGuardException(int value)
    {
        var act = () => WeeklyBalanceGoals.From(value, null);

        act.Should().Throw<GuardException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(8)]
    public void From_MaxRedMeatOutOfRange_ThrowsGuardException(int value)
    {
        var act = () => WeeklyBalanceGoals.From(null, value);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void From_BoundaryValues_CreatesInstance()
    {
        var goals = WeeklyBalanceGoals.From(0, 7);

        goals.MinVeggieMeals.Should().Be(0);
        goals.MaxRedMeatMeals.Should().Be(7);
    }

    [Fact]
    public void None_StaticInstance_IsEmpty()
    {
        WeeklyBalanceGoals.None.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var g1 = WeeklyBalanceGoals.From(2, 3);
        var g2 = WeeklyBalanceGoals.From(2, 3);

        g1.Should().Be(g2);
    }
}
