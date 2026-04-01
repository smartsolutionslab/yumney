using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Shared.Events;

#pragma warning disable SA1601
public sealed partial class InProcessDomainEventDispatcher(IServiceProvider serviceProvider, ILogger<InProcessDomainEventDispatcher> logger)
    : IDomainEventDispatcher
{
#pragma warning disable SA1311
    private static readonly ConcurrentDictionary<Type, (Type HandlerType, MethodInfo Method)> handlerCache = new();
#pragma warning restore SA1311

    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            var eventType = domainEvent.GetType();
            var (handlerType, method) = handlerCache.GetOrAdd(eventType, static type =>
            {
                var ht = typeof(IDomainEventHandler<>).MakeGenericType(type);
                var mi = ht.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!;
                return (ht, mi);
            });

            var handlers = serviceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                LogDispatchingEvent(eventType.Name, handler!.GetType().Name);

                await (Task)method.Invoke(handler!, [domainEvent, cancellationToken])!;
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Dispatching domain event {EventType} to {HandlerType}")]
    private partial void LogDispatchingEvent(string eventType, string handlerType);
}
