namespace SmartSolutionsLab.Yumney.Shopping.Application.DTOs;

public sealed record MergedShoppingItemDto(
    string ItemName,
    decimal TotalQuantity,
    decimal DisplayQuantity,
    string? Unit,
    string Category,
    bool IsBought,
    IReadOnlyList<ItemSourceDto> Sources);

public sealed record ItemSourceDto(
    decimal Quantity,
    string Source,
    DateTime OccurredAt);

public sealed record MergedShoppingListDto(IReadOnlyList<MergedShoppingItemDto> Items);
