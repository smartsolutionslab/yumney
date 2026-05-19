using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Shared.Paging;

namespace SmartSolutionsLab.Yumney.Shared.Persistence;

public static class PagingExtensions
{
	extension<T>(IQueryable<T> query)
	{
		public async Task<(IReadOnlyList<T> Items, ItemCount TotalCount)> ToPagedListAsync(
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
}
