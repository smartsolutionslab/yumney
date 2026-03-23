using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shared.Events;

namespace SmartSolutionsLab.Yumney.Shared.Events.MassTransit;

/// <summary>
/// Generic MassTransit consumer that delegates to IIntegrationEventHandler&lt;TEvent&gt; implementations.
/// </summary>
/// <typeparam name="TEvent">The integration event type to consume.</typeparam>
public sealed class IntegrationEventConsumer<TEvent>(
    IServiceProvider serviceProvider,
    ILogger<IntegrationEventConsumer<TEvent>> logger) : IConsumer<TEvent>
    where TEvent : class, IIntegrationEvent
{
    public async Task Consume(ConsumeContext<TEvent> context)
    {
        var handlers = serviceProvider.GetServices<IIntegrationEventHandler<TEvent>>();

        foreach (var handler in handlers)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(
                    "Handling integration event {EventType} with {HandlerType}",
                    typeof(TEvent).Name,
                    handler.GetType().Name);
            }

            await handler.HandleAsync(context.Message, context.CancellationToken);
        }
    }
}
