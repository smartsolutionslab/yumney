using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Commands;

public sealed record CreateShoppingListItemCommand(ItemName Name, Amount? Amount, Unit? Unit);
