using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Tests;

internal static class ShoppingListTestData
{
    public static ShoppingList CreateList(string ownerId = "user-123")
    {
        return ShoppingList.Create(
            ShoppingListTitle.From("Test List"),
            OwnerIdentifier.From(ownerId),
            [ShoppingListItem.Create(ItemName.From("Flour"), Quantity.From(500, Unit.Grams))]);
    }

    public static ShoppingList CreateListWithItems(string ownerId = "user-123")
    {
        return ShoppingList.Create(
            ShoppingListTitle.From("Test List"),
            OwnerIdentifier.From(ownerId),
            [
                ShoppingListItem.Create(ItemName.From("Milk"), Quantity.From(1, Unit.Liters)),
                ShoppingListItem.Create(ItemName.From("Flour"), Quantity.From(500, Unit.Grams)),
                ShoppingListItem.Create(ItemName.From("Eggs"), Quantity.From(6, null)),
            ]);
    }

    public static ShoppingList CreateListWithItem(string ownerId, out ShoppingListItemIdentifier itemId)
    {
        var item = ShoppingListItem.Create(ItemName.From("Milk"), Quantity.From(1, Unit.Liters));
        var list = ShoppingList.Create(
            ShoppingListTitle.From("Test List"),
            OwnerIdentifier.From(ownerId),
            [item]);
        itemId = item.Id;
        return list;
    }
}
