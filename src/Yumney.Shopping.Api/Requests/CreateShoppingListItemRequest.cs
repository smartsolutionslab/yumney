namespace SmartSolutionsLab.Yumney.Shopping.Api.Requests;

public sealed record CreateShoppingListItemRequest(string Name, decimal? Amount, string? Unit);
