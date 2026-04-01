using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Commands;

public sealed record ShoppingListItem(
    ItemName Name,
    Quantity? Quantity);
