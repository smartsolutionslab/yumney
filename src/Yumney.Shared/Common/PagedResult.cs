namespace SmartSolutionsLab.Yumney.Shared.Common;

public static class PagedResult
{
    public static PagedResult<T> From<T>(IReadOnlyList<T> items, ItemCount totalCount, PagingOptions paging)
        => new(items, totalCount.Value, paging.Page.Value, paging.PageSize.Value);
}

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize);
