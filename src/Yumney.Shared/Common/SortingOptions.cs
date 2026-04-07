namespace SmartSolutionsLab.Yumney.Shared.Common;

public sealed record SortingOptions<TSortField>(TSortField SortBy, SortDirection Direction)
    where TSortField : struct, Enum
{
    /// <summary>
    /// Parses a sort-by query string against the enum, falling back to the
    /// supplied default when the value is missing or unknown.
    /// </summary>
    public static SortingOptions<TSortField> Parse(
        string? sortBy,
        SortDirection direction,
        TSortField fallback)
    {
        var field = default(TSortField).ParseNullable(sortBy) ?? fallback;
        return new SortingOptions<TSortField>(field, direction);
    }
}
