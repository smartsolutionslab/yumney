using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;

public static class ShoppingListSortingExtensions
{
	public static IQueryable<ShoppingListSummaryReadItem> ApplySorting(
		this IQueryable<ShoppingListSummaryReadItem> query,
		SortingOptions<ShoppingListSortField> sorting)
	{
		return (sorting.SortBy, sorting.Direction) switch
		{
			(ShoppingListSortField.Title, SortDirection.Ascending) => query.OrderBy(summary => summary.Title),
			(ShoppingListSortField.Title, SortDirection.Descending) => query.OrderByDescending(summary => summary.Title),
			(ShoppingListSortField.Date, SortDirection.Ascending) => query.OrderBy(summary => summary.CreatedAt),
			(ShoppingListSortField.Date, SortDirection.Descending) => query.OrderByDescending(summary => summary.CreatedAt),
			_ => throw new InvalidOperationException($"Unsupported sort combination: {sorting.SortBy}, {sorting.Direction}"),
		};
	}
}
