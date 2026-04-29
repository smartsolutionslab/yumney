namespace SmartSolutionsLab.Yumney.Shopping.Application.DTOs;

public sealed record IngredientBalanceDto(IReadOnlyList<IngredientBalanceItemDto> Items);
