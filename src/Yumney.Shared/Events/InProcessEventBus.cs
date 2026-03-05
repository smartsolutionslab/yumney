using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Yumney.Shared.Events;

public sealed class InProcessEventBus : IEventBus
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<InProcessEventBus> logger;

    public InProcessEventBus(
        IServiceProvider serviceProvider,
        ILogger<InProcessEventBus> logger)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
    }

    public async Task PublishAsync<TEvent>(
        TEvent integrationEvent,
        CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        var handlers = serviceProvider.GetServices<IIntegrationEventHandler<TEvent>>();

        foreach (var handler in handlers)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(
                    "Publishing integration event {EventType} to {HandlerType}",
                    typeof(TEvent).Name,
                    handler.GetType().Name);
            }

            await handler.HandleAsync(integrationEvent, cancellationToken);
        }
    }
}
