namespace SmartSolutionsLab.Yumney.Shopping.Application.DTOs;

public sealed record ShoppingListItemDto(
    Guid Identifier,
    string Name,
    decimal? Amount,
    string? Unit,
    bool IsChecked);
