using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.TestBuilders.Shopping;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Tests;

internal static class ShoppingListTestData
{
	public static ShoppingList CreateList(string ownerId = "user-123") =>
		ShoppingListBuilder.A()
			.OwnedBy(ownerId)
			.WithItems([ShoppingListItemBuilder.A().Named("Flour").WithQuantity(500, Unit.Gram)])
			.Build();

	public static ShoppingList CreateListWithItems(string ownerId = "user-123") =>
		ShoppingListBuilder.A()
			.OwnedBy(ownerId)
			.WithItems([
				ShoppingListItemBuilder.A().Named("Milk").WithQuantity(1, Unit.Liter),
				ShoppingListItemBuilder.A().Named("Flour").WithQuantity(500, Unit.Gram),
				ShoppingListItemBuilder.A().Named("Eggs").WithQuantity(6, null),
			])
			.Build();

	public static ShoppingList CreateListWithItem(string ownerId, out ShoppingListItemIdentifier itemId)
	{
		var item = ShoppingListItemBuilder.A().Named("Milk").WithQuantity(1, Unit.Liter).Build();
		var list = ShoppingListBuilder.A().OwnedBy(ownerId).WithItems([item]).Build();
		itemId = item.Id;
		return list;
	}
}
