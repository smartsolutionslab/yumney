using System.Collections.Generic;

namespace SmartSolutionsLab.Yumney.Shared.Paging;

public static class PagedResultExtensions
{
	public static PagedResult<T> With<T>(IReadOnlyList<T> items, ItemCount totalCount, PagingOptions paging)
		=> new(items, totalCount.Value, paging.Page.Value, paging.PageSize.Value);
}
