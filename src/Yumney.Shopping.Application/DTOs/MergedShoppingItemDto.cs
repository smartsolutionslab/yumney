namespace SmartSolutionsLab.Yumney.Shopping.Application.DTOs;

public sealed record MergedShoppingItemDto(
    string ItemName,
    decimal TotalQuantity,
    decimal DisplayQuantity,
    string? Unit,
    string Category,
    bool IsBought,
    IReadOnlyList<ItemSourceDto> Sources);
