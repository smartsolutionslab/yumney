namespace SmartSolutionsLab.Yumney.Shopping.Application.Commands;

public sealed record CreateShoppingListItemRequest(string Name, decimal? Amount, string? Unit);
