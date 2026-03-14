using FluentAssertions;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.Tests.ShoppingList;

public class ShoppingListItemTests
{
    [Fact]
    public void Create_WithAllFields_SetsProperties()
    {
        var name = new ItemName("Flour");
        var amount = new Amount(500);
        var unit = new Unit("g");

        var item = ShoppingListItem.Create(name, amount, unit);

        item.Id.Should().NotBeEmpty();
        item.Name.Should().Be(name);
        item.Amount.Should().Be(amount);
        item.Unit.Should().Be(unit);
    }

    [Fact]
    public void Create_WithoutAmount_AmountIsNull()
    {
        var item = ShoppingListItem.Create(new ItemName("Salt"), null, null);

        item.Amount.Should().BeNull();
    }

    [Fact]
    public void Create_WithoutUnit_UnitIsNull()
    {
        var item = ShoppingListItem.Create(new ItemName("Eggs"), new Amount(3), null);

        item.Unit.Should().BeNull();
        item.Amount!.Value.Should().Be(3);
    }
}
