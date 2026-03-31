using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Shared.Events;

public sealed class InProcessDomainEventDispatcher(IServiceProvider serviceProvider, ILogger<InProcessDomainEventDispatcher> logger)
    : IDomainEventDispatcher
{
    private static readonly ConcurrentDictionary<Type, (Type HandlerType, MethodInfo Method)> HandlerCache = new();

    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            var eventType = domainEvent.GetType();
            var (handlerType, method) = HandlerCache.GetOrAdd(eventType, static type =>
            {
                var ht = typeof(IDomainEventHandler<>).MakeGenericType(type);
                var mi = ht.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!;
                return (ht, mi);
            });

            var handlers = serviceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug(
                        "Dispatching domain event {EventType} to {HandlerType}",
                        eventType.Name,
                        handler!.GetType().Name);
                }

                await (Task)method.Invoke(handler!, [domainEvent, cancellationToken])!;
            }
        }
    }
}
