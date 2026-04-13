using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;

public static class ShoppingListFactory
{
    public static ShoppingList WeeklyGroceries(string? owner = null) => ShoppingList.Create(
        ShoppingListTitle.From("Weekly Groceries"),
        OwnerIdentifier.From(owner ?? "integration-test-user"),
        [
            ShoppingListItem.Create(ItemName.From("Milk"), Quantity.Of(Amount.From(2), Unit.Liters)),
            ShoppingListItem.Create(ItemName.From("Bread"), Quantity.Of(Amount.From(1), null)),
            ShoppingListItem.Create(ItemName.From("Eggs"), Quantity.Of(Amount.From(12), null)),
            ShoppingListItem.Create(ItemName.From("Butter"), Quantity.Of(Amount.From(250), Unit.Grams)),
        ]);

    public static ShoppingList PartySupplies(string? owner = null) => ShoppingList.Create(
        ShoppingListTitle.From("Party Supplies"),
        OwnerIdentifier.From(owner ?? "integration-test-user"),
        [
            ShoppingListItem.Create(ItemName.From("Chips"), Quantity.Of(Amount.From(3), Unit.From("bags"))),
            ShoppingListItem.Create(ItemName.From("Soda"), Quantity.Of(Amount.From(6), Unit.From("bottles"))),
            ShoppingListItem.Create(ItemName.From("Napkins"), Quantity.Of(Amount.From(1), Unit.From("pack"))),
        ]);

    public static ShoppingList BakingIngredients(string? owner = null) => ShoppingList.Create(
        ShoppingListTitle.From("Baking Ingredients"),
        OwnerIdentifier.From(owner ?? "integration-test-user"),
        [
            ShoppingListItem.Create(ItemName.From("Flour"), Quantity.Of(Amount.From(1000), Unit.Grams)),
            ShoppingListItem.Create(ItemName.From("Sugar"), Quantity.Of(Amount.From(500), Unit.Grams)),
        ],
        RecipeReference.From(Guid.NewGuid()));
}
