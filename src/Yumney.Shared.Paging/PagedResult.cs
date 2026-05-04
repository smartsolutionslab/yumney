using System.Collections.Generic;

namespace SmartSolutionsLab.Yumney.Shared.Paging;

public sealed record PagedResult<T>(
	IReadOnlyList<T> Items,
	int TotalCount,
	int Page,
	int PageSize);
