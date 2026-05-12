using SmartSolutionsLab.Yumney.Shared.Quantities;

namespace SmartSolutionsLab.Yumney.Shopping.Application.DTOs;

public static class IngredientBalanceMappingExtensions
{
	public static IngredientBalanceItemDto ToStapleBalanceItem(this IngredientCategory category, string stapleName) =>
		new(
			ItemName: stapleName,
			Quantity: null,
			Unit: null,
			Category: category.Value,
			Source: IngredientBalanceSource.Staple);
}
