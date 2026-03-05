using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Yumney.Shared.Common;

namespace Yumney.Shared.Events;

public sealed class InProcessDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<InProcessDomainEventDispatcher> logger;

    public InProcessDomainEventDispatcher(
        IServiceProvider serviceProvider,
        ILogger<InProcessDomainEventDispatcher> logger)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
    }

    public async Task DispatchAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            var eventType = domainEvent.GetType();
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
            var handlers = serviceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                var method = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync));

                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug(
                        "Dispatching domain event {EventType} to {HandlerType}",
                        eventType.Name,
                        handler!.GetType().Name);
                }

                await (Task)method!.Invoke(handler!, [domainEvent, cancellationToken])!;
            }
        }
    }
}
