namespace SmartSolutionsLab.Yumney.Shared.Paging;

public sealed record PagedResult<TItem>(
	IReadOnlyList<TItem> Items,
	int TotalCount,
	int Page,
	int PageSize);
