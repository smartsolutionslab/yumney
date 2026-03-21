namespace SmartSolutionsLab.Yumney.Shopping.Application.Requests;

public sealed record CreateShoppingListItemRequest(string Name, decimal? Amount, string? Unit);
