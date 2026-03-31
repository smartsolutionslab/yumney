namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

public sealed record ShoppingListSummary(
    ShoppingListIdentifier Identifier,
    ShoppingListTitle Title,
    int ItemCount,
    DateTime CreatedAt);
