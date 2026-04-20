using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace SmartSolutionsLab.Yumney.Shared.Persistence;

/// <summary>
/// EF Core command interceptor that increments a scoped
/// <see cref="IQueryCounter"/> for every reader, non-query, and scalar
/// command execution. Intended for integration tests and diagnostic
/// builds; attach only where the counter is scoped to a single logical
/// operation.
/// </summary>
public sealed class QueryCountingInterceptor(IQueryCounter counter) : DbCommandInterceptor
{
	public override InterceptionResult<DbDataReader> ReaderExecuting(
		DbCommand command,
		CommandEventData eventData,
		InterceptionResult<DbDataReader> result)
	{
		counter.Increment();
		return base.ReaderExecuting(command, eventData, result);
	}

	public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
		DbCommand command,
		CommandEventData eventData,
		InterceptionResult<DbDataReader> result,
		CancellationToken cancellationToken = default)
	{
		counter.Increment();
		return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
	}

	public override InterceptionResult<int> NonQueryExecuting(
		DbCommand command,
		CommandEventData eventData,
		InterceptionResult<int> result)
	{
		counter.Increment();
		return base.NonQueryExecuting(command, eventData, result);
	}

	public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
		DbCommand command,
		CommandEventData eventData,
		InterceptionResult<int> result,
		CancellationToken cancellationToken = default)
	{
		counter.Increment();
		return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
	}

	public override InterceptionResult<object> ScalarExecuting(
		DbCommand command,
		CommandEventData eventData,
		InterceptionResult<object> result)
	{
		counter.Increment();
		return base.ScalarExecuting(command, eventData, result);
	}

	public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
		DbCommand command,
		CommandEventData eventData,
		InterceptionResult<object> result,
		CancellationToken cancellationToken = default)
	{
		counter.Increment();
		return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
	}
}
