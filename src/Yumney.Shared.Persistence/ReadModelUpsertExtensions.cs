using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace SmartSolutionsLab.Yumney.Shared.Persistence;

public static class ReadModelUpsertExtensions
{
	public static async Task UpsertAsync<TEntity>(
		this DbContext context,
		Expression<Func<TEntity, bool>> predicate,
		Func<TEntity> create,
		Action<TEntity> mutate,
		CancellationToken cancellationToken = default)
		where TEntity : class
	{
		var row = await context.Set<TEntity>().FirstOrDefaultAsync(predicate, cancellationToken);
		if (row is null)
		{
			row = create();
			context.Set<TEntity>().Add(row);
		}

		mutate(row);
		await context.SaveChangesAsync(cancellationToken);
	}

	public static async Task UpdateAsync<TEntity>(
		this DbContext context,
		Expression<Func<TEntity, bool>> predicate,
		Action<TEntity> mutate,
		CancellationToken cancellationToken = default)
		where TEntity : class
	{
		var row = await context.Set<TEntity>().FirstOrDefaultAsync(predicate, cancellationToken);
		if (row is null) return;

		mutate(row);
		await context.SaveChangesAsync(cancellationToken);
	}
}
