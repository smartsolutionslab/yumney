using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SmartSolutionsLab.Yumney.Shared.Events;

#pragma warning disable SA1601
public sealed partial class InProcessEventBus(IServiceProvider serviceProvider, ILogger<InProcessEventBus> logger) : IEventBus
{
    public async Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        var handlers = serviceProvider.GetServices<IIntegrationEventHandler<TEvent>>();
        var eventName = typeof(TEvent).Name;

        foreach (var handler in handlers)
        {
            var handlerName = handler.GetType().Name;
            using var activity = EventsDiagnostics.ActivitySource.StartActivity($"integration_event.{eventName}.{handlerName}");
            activity?.SetTag("event.name", eventName);
            activity?.SetTag("handler.name", handlerName);

            LogPublishingEvent(eventName, handlerName);
            var start = Stopwatch.GetTimestamp();

            try
            {
                await handler.HandleAsync(integrationEvent, cancellationToken);
                var elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                activity?.SetTag("event.result", "success");
                activity?.SetStatus(ActivityStatusCode.Ok);
                LogHandlerCompleted(eventName, handlerName, elapsed);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                var elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                activity?.SetTag("event.result", "error");
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

                // Isolate handlers — one failing subscriber must not block the others.
                LogHandlerFailed(ex, eventName, handlerName, elapsed);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Publishing integration event {EventType} to {HandlerType}")]
    private partial void LogPublishingEvent(string eventType, string handlerType);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Handled integration event {EventType} in {HandlerType} in {ElapsedMs:F1}ms")]
    private partial void LogHandlerCompleted(string eventType, string handlerType, double elapsedMs);

    [LoggerMessage(Level = LogLevel.Error, Message = "Integration event handler {HandlerType} failed for {EventType} after {ElapsedMs:F1}ms")]
    private partial void LogHandlerFailed(Exception ex, string eventType, string handlerType, double elapsedMs);
}
