namespace SmartSolutionsLab.Yumney.Shopping.Application.DTOs;

/// <summary>
/// One ingredient in the balance sheet ("what's at home right now").
/// <see cref="Quantity"/> is null for staples that are tracked as
/// "always available" rather than as a measurable amount.
/// </summary>
public sealed record IngredientBalanceItemDto(
	string ItemName,
	decimal? Quantity,
	string? Unit,
	string Category,
	IngredientBalanceSource Source);
