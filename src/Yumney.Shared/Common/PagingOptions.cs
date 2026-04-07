namespace SmartSolutionsLab.Yumney.Shared.Common;

public sealed record PagingOptions
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public Page Page { get; }

    public PageSize PageSize { get; }

    public int Skip => Page.SkipCount(PageSize);

    private PagingOptions(Page page, PageSize pageSize)
    {
        Page = page;
        PageSize = pageSize;
    }

    public static PagingOptions Of(Page page, PageSize pageSize) => new(page, pageSize);

    /// <summary>
    /// Convenience factory for endpoint query-parameter parsing — wraps the
    /// raw page/pageSize ints in their value objects.
    /// </summary>
    public static PagingOptions From(int page, int pageSize) =>
        new(Page.From(page), PageSize.From(pageSize));
}
