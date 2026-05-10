using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shopping.Client;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.ExternalServices;

internal static class ShoppingListLookupMappingExtensions
{
	public static ShoppingListLookupResult ToLookupResult(this MergedShoppingListResponse response) =>
		new([.. response.Items.Select(item => item.ToLookupItem())]);

	public static ShoppingListLookupItem ToLookupItem(this MergedShoppingListItemResponse item) =>
		new(item.ItemName, item.DisplayQuantity, item.Unit, item.Category, item.IsBought);
}
