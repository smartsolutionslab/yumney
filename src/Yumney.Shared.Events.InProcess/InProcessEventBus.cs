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

        foreach (var handler in handlers)
        {
            LogPublishingEvent(typeof(TEvent).Name, handler.GetType().Name);

            await handler.HandleAsync(integrationEvent, cancellationToken);
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Publishing integration event {EventType} to {HandlerType}")]
    private partial void LogPublishingEvent(string eventType, string handlerType);
}
