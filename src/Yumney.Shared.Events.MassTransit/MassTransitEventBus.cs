using MassTransit;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shared.Events;

namespace SmartSolutionsLab.Yumney.Shared.Events.MassTransit;

/// <summary>
/// IEventBus implementation that publishes integration events via MassTransit/RabbitMQ.
/// </summary>
public sealed class MassTransitEventBus(IPublishEndpoint publishEndpoint, ILogger<MassTransitEventBus> logger) : IEventBus
{
    public async Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Publishing integration event {EventType} via MassTransit", typeof(TEvent).Name);
        }

        await publishEndpoint.Publish(integrationEvent, cancellationToken);
    }
}
