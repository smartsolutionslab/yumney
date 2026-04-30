using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.DTOs;

public static class ShoppingListMappingExtensions
{
	public static ShoppingListDetailDto ToDetailDto(this ShoppingList shoppingList) =>
		new(
			shoppingList.Identifier.Value,
			shoppingList.Title.Value,
			shoppingList.RecipeReference?.Value,
			shoppingList.CreatedAt,
			shoppingList.Items.ToDtos());

	public static ShoppingListSummaryDto ToSummaryDto(this ShoppingListSummary summary) =>
		new(
			summary.Identifier.Value,
			summary.Title.Value,
			summary.ItemCount.Value,
			summary.CreatedAt);

	public static IReadOnlyList<ShoppingListSummaryDto> ToSummaryDtos(this IEnumerable<ShoppingListSummary> summaries) =>
		summaries.Select(summary => summary.ToSummaryDto()).ToList();

	public static ShoppingListItemDto ToDto(this ShoppingListItem item) =>
		new(
			item.Id.Value,
			item.Name.Value,
			item.Quantity?.Amount.Value,
			item.Quantity?.Unit?.Value,
			item.IsChecked);

	public static IReadOnlyList<ShoppingListItemDto> ToDtos(this IEnumerable<ShoppingListItem> items) =>
		items.Select(item => item.ToDto()).ToList();
}
