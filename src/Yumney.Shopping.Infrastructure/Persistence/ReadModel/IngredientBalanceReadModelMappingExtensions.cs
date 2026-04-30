using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;

public static class IngredientBalanceReadModelMappingExtensions
{
	public static IngredientBalanceItemDto ToDto(
		this IngredientBalanceReadItem row,
		Freshness freshness,
		int? daysSinceBought) =>
		new(
			ItemName: row.ItemName,
			Quantity: row.AtHome,
			Unit: row.Unit,
			Category: row.Category,
			Source: IngredientBalanceSource.AtHome,
			Freshness: freshness,
			DaysSinceBought: daysSinceBought);
}
