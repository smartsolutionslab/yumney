using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;

public static class ShoppingListReadModelMappingExtensions
{
	public static ShoppingListItemDto ToDto(this ShoppingListItemReadItem row) =>
		new(row.Id, row.Name, row.QuantityAmount, row.QuantityUnit, row.Category, row.IsChecked);

	public static IReadOnlyList<ShoppingListItemDto> ToDtos(this IEnumerable<ShoppingListItemReadItem> rows) =>
		rows.Select(row => row.ToDto()).ToList();

	public static ShoppingListDetailDto ToDetailDto(this ShoppingListSummaryReadItem summary, IReadOnlyList<ShoppingListItemDto> items) =>
		new(
			summary.Id,
			summary.Title,
			summary.RecipeIdentifier,
			summary.CreatedAt,
			items);
}
