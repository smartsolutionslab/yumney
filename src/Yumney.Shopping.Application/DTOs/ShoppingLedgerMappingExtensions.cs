using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.DTOs;

public static class ShoppingLedgerMappingExtensions
{
	extension(ShoppingLedger ledger)
	{
		public AddedItemDto ToAddedItemDto(ItemName itemName, Quantity quantity, IngredientCategory category, ItemSource source)
		{
			return new AddedItemDto(
				itemName.Value,
				quantity.Amount,
				quantity.Unit?.Value,
				category.Value,
				source.Value,
				ledger.Identifier);
		}
	}
}
