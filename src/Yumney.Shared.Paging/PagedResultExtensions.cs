namespace SmartSolutionsLab.Yumney.Shared.Paging;

public static class PagedResultExtensions
{
	public static PagedResult<T> With<T>(this IReadOnlyList<T> items, ItemCount totalCount, PagingOptions paging)
		=> new(items, totalCount.Value, paging.Page.Value, paging.PageSize.Value);

	public static PagedResult<TResult> Map<TSource, TResult>(this PagedResult<TSource> source, Func<TSource, TResult> selector)
		=> new(
			source.Items.Select(selector).ToList(),
			source.TotalCount,
			source.Page,
			source.PageSize);
}
