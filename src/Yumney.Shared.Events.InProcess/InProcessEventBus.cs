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
            LogPublishingEvent(eventName, handlerName);

            try
            {
                await handler.HandleAsync(integrationEvent, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Isolate handlers — one failing subscriber must not block the others.
                LogHandlerFailed(ex, eventName, handlerName);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Publishing integration event {EventType} to {HandlerType}")]
    private partial void LogPublishingEvent(string eventType, string handlerType);

    [LoggerMessage(Level = LogLevel.Error, Message = "Integration event handler {HandlerType} failed for {EventType}")]
    private partial void LogHandlerFailed(Exception ex, string eventType, string handlerType);
}
