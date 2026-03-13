namespace SmartSolutionsLab.Yumney.Shared.Common;

public sealed record SortingOptions<TSortField>(TSortField SortBy, SortDirection Direction)
    where TSortField : struct, Enum;
