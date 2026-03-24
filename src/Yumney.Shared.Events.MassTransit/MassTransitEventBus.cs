using MassTransit;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shared.Events;

namespace SmartSolutionsLab.Yumney.Shared.Events.MassTransit;

/// <summary>
/// IEventBus implementation that publishes integration events via MassTransit/RabbitMQ.
/// </summary>
public sealed partial class MassTransitEventBus(IPublishEndpoint publishEndpoint, ILogger<MassTransitEventBus> logger) : IEventBus
{
    public async Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            LogPublishingIntegrationEventEventtypeViaMasstransit(logger, typeof(TEvent).Name);
        }

        await publishEndpoint.Publish(integrationEvent, cancellationToken);
    }

    [LoggerMessage(LogLevel.Debug, "Publishing integration event {EventType} via MassTransit")]
    static partial void LogPublishingIntegrationEventEventtypeViaMasstransit(ILogger<MassTransitEventBus> logger, string EventType);
}
