using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Tests;

internal static class ShoppingListTestData
{
    public static ShoppingList CreateList(string ownerId = "user-123")
    {
        return ShoppingList.Create(
            ShoppingListTitle.From("Test List"),
            OwnerIdentifier.From(ownerId),
            [ShoppingListItem.Create(ItemName.From("Flour"), Quantity.Of(Amount.From(500), Unit.From("g")))]);
    }

    public static ShoppingList CreateListWithItems(string ownerId = "user-123")
    {
        return ShoppingList.Create(
            ShoppingListTitle.From("Test List"),
            OwnerIdentifier.From(ownerId),
            [
                ShoppingListItem.Create(ItemName.From("Milk"), Quantity.Of(Amount.From(1), Unit.From("l"))),
                ShoppingListItem.Create(ItemName.From("Flour"), Quantity.Of(Amount.From(500), Unit.From("g"))),
                ShoppingListItem.Create(ItemName.From("Eggs"), Quantity.Of(Amount.From(6), null)),
            ]);
    }

    public static ShoppingList CreateListWithItem(string ownerId, out ShoppingListItemIdentifier itemId)
    {
        var item = ShoppingListItem.Create(ItemName.From("Milk"), Quantity.Of(Amount.From(1), Unit.From("l")));
        var list = ShoppingList.Create(
            ShoppingListTitle.From("Test List"),
            OwnerIdentifier.From(ownerId),
            [item]);
        itemId = item.Id;
        return list;
    }
}
