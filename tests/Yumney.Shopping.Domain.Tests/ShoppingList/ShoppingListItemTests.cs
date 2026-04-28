using FluentAssertions;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.Tests.ShoppingList;

public class ShoppingListItemTests
{
	[Fact]
	public void Create_WithAllFields_SetsProperties()
	{
		var name = ItemName.From("Flour");
		var amount = Amount.From(500);
		var unit = Unit.Gram;

		var item = ShoppingListItem.Create(name, Quantity.Of(amount, unit));

		item.Id.Should().NotBeNull();
		item.Name.Should().Be(name);
		item.Quantity.Should().NotBeNull();
		item.Quantity!.Amount.Should().Be(amount);
		item.Quantity.Unit.Should().Be(unit);
	}

	[Fact]
	public void Create_WithoutAmount_AmountIsNull()
	{
		var item = ShoppingListItem.Create(ItemName.From("Salt"), null);

		item.Quantity.Should().BeNull();
	}

	[Fact]
	public void Create_WithoutUnit_UnitIsNull()
	{
		var amount = Amount.From(3);

		var item = ShoppingListItem.Create(ItemName.From("Eggs"), Quantity.Of(amount, null));

		item.Quantity.Should().NotBeNull();
		item.Quantity!.Unit.Should().BeNull();
		item.Quantity.Amount.Should().Be(amount);
	}

	[Fact]
	public void Create_SetsIsCheckedToFalse()
	{
		var item = CreateFlourItem();

		item.IsChecked.Should().BeFalse();
	}

	private static ShoppingListItem CreateFlourItem() =>
		ShoppingListItem.Create(ItemName.From("Flour"), Quantity.Of(Amount.From(500), Unit.Gram));
}
