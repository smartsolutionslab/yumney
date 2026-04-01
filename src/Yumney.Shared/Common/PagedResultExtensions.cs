namespace SmartSolutionsLab.Yumney.Shared.Common;

public static class PagedResultExtensions
{
    public static PagedResult<T> With<T>(IReadOnlyList<T> items, ItemCount totalCount, PagingOptions paging)
        => new(items, totalCount.Value, paging.Page.Value, paging.PageSize.Value);
}
