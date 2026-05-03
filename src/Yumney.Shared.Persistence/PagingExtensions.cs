using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Shared.Persistence;

public static class PagingExtensions
{
	public static async Task<(IReadOnlyList<T> Items, ItemCount TotalCount)> ToPagedListAsync<T>(
		this IQueryable<T> query,
		PagingOptions paging,
		CancellationToken cancellationToken = default)
	{
		var totalCount = await query.CountAsync(cancellationToken);
		var items = await query
			.Skip(paging.Skip)
			.Take(paging.PageSize.Value)
			.ToListAsync(cancellationToken);
		return (items, ItemCount.From(totalCount));
	}
}
