namespace SmartSolutionsLab.Yumney.Shopping.Application.DTOs;

public sealed record MergedShoppingListDto(IReadOnlyList<MergedShoppingItemDto> Items);
