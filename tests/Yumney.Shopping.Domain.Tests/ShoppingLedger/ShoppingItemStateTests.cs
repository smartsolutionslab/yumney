using FluentAssertions;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.Tests.ShoppingLedger;

public class ShoppingItemStateTests
{
    [Fact]
    public void AtHome_BoughtMinusConsumedAndRemoved()
    {
        var state = CreateState();
        state.Bought = 10;
        state.Consumed = 3;
        state.Removed = 2;

        state.AtHome.Should().Be(5);
    }

    [Fact]
    public void AtHome_NeverNegative()
    {
        var state = CreateState();
        state.Bought = 1;
        state.Consumed = 5;
        state.Removed = 3;

        state.AtHome.Should().Be(0);
    }

    [Fact]
    public void AtHome_ZeroWhenNothingBought()
    {
        var state = CreateState();

        state.AtHome.Should().Be(0);
    }

    [Fact]
    public void Remaining_OnListMinusBought()
    {
        var state = CreateState();
        state.OnList = 5;
        state.Bought = 2;

        state.Remaining.Should().Be(3);
    }

    [Fact]
    public void Remaining_CanBeNegative_WhenOverbought()
    {
        var state = CreateState();
        state.OnList = 2;
        state.Bought = 5;

        state.Remaining.Should().Be(-3);
    }

    [Fact]
    public void IsBought_True_WhenBoughtPositive()
    {
        var state = CreateState();
        state.Bought = 1;

        state.IsBought.Should().BeTrue();
    }

    [Fact]
    public void IsBought_False_WhenBoughtZero()
    {
        var state = CreateState();

        state.IsBought.Should().BeFalse();
    }

    [Fact]
    public void GroupKey_CombinesItemNameAndUnit()
    {
        var state = CreateState(unit: Unit.From("L"));

        state.GroupKey.Should().Be("milk|L");
    }

    [Fact]
    public void GroupKey_CaseInsensitiveItemName()
    {
        var state = new ShoppingItemState { ItemName = ItemName.From("MILK"), Unit = Unit.From("L") };

        state.GroupKey.Should().Be("milk|L");
    }

    [Fact]
    public void GroupKey_NullUnit_UsesEmptyString()
    {
        var state = CreateState(unit: null);

        state.GroupKey.Should().Be("milk|");
    }

    private static ShoppingItemState CreateState(Unit? unit = null) =>
        new() { ItemName = ItemName.From("Milk"), Unit = unit };
}
