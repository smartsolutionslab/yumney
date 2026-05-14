using SmartSolutionsLab.Yumney.Shared.Quantities;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;

public static class ShoppingLedgerReadModelMappingExtensions
{
	public static MergedShoppingItemDto ToMergedItemDto(this ShoppingLedgerReadItem row, IReadOnlyList<ItemSourceDto> sources)
	{
		var rounded = QuantityRounder.RoundUp(row.TotalQuantity, row.Unit);
		return new(
			row.ItemName,
			row.TotalQuantity,
			rounded.DisplayQuantity,
			row.Unit,
			row.Category,
			row.IsBought,
			sources);
	}

	public static MergedShoppingListDto ToMergedListDto(this IReadOnlyList<MergedShoppingItemDto> items) =>
		new(items);
}
