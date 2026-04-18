using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.DTOs;

public static class ShoppingListMappingExtensions
{
	extension(ShoppingList shoppingList)
	{
		public ShoppingListDetailDto ToDetailDto()
		{
			return new ShoppingListDetailDto(
				shoppingList.Id.Value,
				shoppingList.Title.Value,
				shoppingList.RecipeReference?.Value,
				shoppingList.CreatedAt,
				shoppingList.Items.Select(i => i.ToDto()).ToList());
		}
	}

	public static ShoppingListSummaryDto ToSummaryDto(this ShoppingListSummary summary)
	{
		return new ShoppingListSummaryDto(
			summary.Identifier.Value,
			summary.Title.Value,
			summary.ItemCount.Value,
			summary.CreatedAt);
	}

	public static ShoppingListItemDto ToDto(this ShoppingListItem item)
	{
		return new ShoppingListItemDto(
			item.Id.Value,
			item.Name.Value,
			item.Quantity?.Amount.Value,
			item.Quantity?.Unit?.Value,
			item.IsChecked);
	}
}
