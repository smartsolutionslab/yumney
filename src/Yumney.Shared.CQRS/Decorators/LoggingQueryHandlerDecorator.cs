using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shared.CQRS.Diagnostics;

namespace SmartSolutionsLab.Yumney.Shared.CQRS.Decorators;

#pragma warning disable SA1601 // Partial elements should be documented (LoggerMessage generates partial methods)
#pragma warning disable SA1311 // Static readonly fields should begin with upper-case letter (editorconfig requires camelCase)
public sealed partial class LoggingQueryHandlerDecorator<TQuery, TResult>(
	IQueryHandler<TQuery, TResult> inner,
	ILogger<LoggingQueryHandlerDecorator<TQuery, TResult>> logger,
	ApplicationMetrics metrics)
	: IQueryHandler<TQuery, TResult>
	where TQuery : IQuery<TResult>
{
	private static readonly string queryName = typeof(TQuery).Name;
	private readonly string handlerName = inner.GetType().Name;

	public async Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default)
	{
		using var activity = ApplicationDiagnostics.ActivitySource.StartActivity($"query.{queryName}");
		activity?.SetTag("handler.name", handlerName);
		activity?.SetTag("handler.query_type", queryName);

		LogHandling(queryName, handlerName);
		var start = Stopwatch.GetTimestamp();

		try
		{
			var result = await inner.HandleAsync(query, cancellationToken);
			var elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;

			if (ResultInspector.IsFailure(result, out var errorCode, out var errorMessage))
			{
				activity?.SetTag("handler.result", "failure");
				activity?.SetStatus(ActivityStatusCode.Error, errorMessage);
				LogFailed(queryName, handlerName, elapsed, errorCode!, errorMessage!);
				metrics.RecordExecution(handlerName, queryName, "failure", elapsed);
			}
			else
			{
				activity?.SetTag("handler.result", "success");
				activity?.SetStatus(ActivityStatusCode.Ok);
				LogHandled(queryName, handlerName, elapsed);
				metrics.RecordExecution(handlerName, queryName, "success", elapsed);
			}

			return result;
		}
		catch (Exception ex)
		{
			var elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
			activity?.SetTag("handler.result", "exception");
			activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
			LogException(ex, queryName, handlerName, elapsed);
			metrics.RecordExecution(handlerName, queryName, "exception", elapsed);
			throw;
		}
	}

	[LoggerMessage(Level = LogLevel.Debug, Message = "Handling {queryName} [{handlerName}]")]
	private partial void LogHandling(string queryName, string handlerName);

	[LoggerMessage(Level = LogLevel.Debug, Message = "Handled {queryName} [{handlerName}] in {ElapsedMs:F1}ms")]
	private partial void LogHandled(string queryName, string handlerName, double elapsedMs);

	[LoggerMessage(Level = LogLevel.Warning, Message = "Failed {queryName} [{handlerName}] in {ElapsedMs:F1}ms — {ErrorCode}: {ErrorMessage}")]
	private partial void LogFailed(string queryName, string handlerName, double elapsedMs, string errorCode, string errorMessage);

	[LoggerMessage(Level = LogLevel.Error, Message = "Exception in {queryName} [{handlerName}] after {ElapsedMs:F1}ms")]
	private partial void LogException(Exception ex, string queryName, string handlerName, double elapsedMs);
}
