namespace SmartSolutionsLab.Yumney.Shared.Common;

public sealed record SortingOptions<TSortField>(TSortField SortBy, SortDirection Direction)
	where TSortField : struct, Enum
{
	public static SortingOptions<TSortField> Parse(
		string? sortBy,
		SortDirection direction,
		TSortField fallback)
	{
		var field = default(TSortField).ParseNullable(sortBy) ?? fallback;
		return new SortingOptions<TSortField>(field, direction);
	}
}
