using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shared.CQRS.Diagnostics;

namespace SmartSolutionsLab.Yumney.Shared.CQRS.Decorators;

#pragma warning disable SA1601 // Partial elements should be documented (LoggerMessage generates partial methods)
#pragma warning disable SA1311 // Static readonly fields should begin with upper-case letter (editorconfig requires camelCase)
public sealed partial class LoggingCommandHandlerDecorator<TCommand, TResult>(
    ICommandHandler<TCommand, TResult> inner,
    ILogger<LoggingCommandHandlerDecorator<TCommand, TResult>> logger,
    ApplicationMetrics metrics)
    : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    private static readonly string commandName = typeof(TCommand).Name;
    private readonly string handlerName = inner.GetType().Name;

    public async Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        using var activity = ApplicationDiagnostics.ActivitySource.StartActivity($"command.{commandName}");
        activity?.SetTag("handler.name", handlerName);
        activity?.SetTag("handler.command_type", commandName);

        LogHandling(commandName, handlerName);
        var start = Stopwatch.GetTimestamp();

        try
        {
            var result = await inner.HandleAsync(command, cancellationToken);
            var elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;

            if (ResultInspector.IsFailure(result, out var errorCode, out var errorMessage))
            {
                activity?.SetTag("handler.result", "failure");
                activity?.SetStatus(ActivityStatusCode.Error, errorMessage);
                LogFailed(commandName, handlerName, elapsed, errorCode!, errorMessage!);
                metrics.RecordExecution(handlerName, commandName, "failure", elapsed);
            }
            else
            {
                activity?.SetTag("handler.result", "success");
                activity?.SetStatus(ActivityStatusCode.Ok);
                LogHandled(commandName, handlerName, elapsed);
                metrics.RecordExecution(handlerName, commandName, "success", elapsed);
            }

            return result;
        }
        catch (Exception ex)
        {
            var elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            activity?.SetTag("handler.result", "exception");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            LogException(ex, commandName, handlerName, elapsed);
            metrics.RecordExecution(handlerName, commandName, "exception", elapsed);
            throw;
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Handling {commandName} [{handlerName}]")]
    private partial void LogHandling(string commandName, string handlerName);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Handled {commandName} [{handlerName}] in {ElapsedMs:F1}ms")]
    private partial void LogHandled(string commandName, string handlerName, double elapsedMs);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed {commandName} [{handlerName}] in {ElapsedMs:F1}ms — {ErrorCode}: {ErrorMessage}")]
    private partial void LogFailed(string commandName, string handlerName, double elapsedMs, string errorCode, string errorMessage);

    [LoggerMessage(Level = LogLevel.Error, Message = "Exception in {commandName} [{handlerName}] after {ElapsedMs:F1}ms")]
    private partial void LogException(Exception ex, string commandName, string handlerName, double elapsedMs);
}
