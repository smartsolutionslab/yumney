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
                var handlerName = handler!.GetType().Name;
                LogDispatchingEvent(eventType.Name, handlerName);

                try
                {
                    await (Task)method.Invoke(handler!, [domainEvent, cancellationToken])!;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // Isolate handlers — one failing handler must not block the others.
                    LogHandlerFailed(ex, eventType.Name, handlerName);
                }
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Dispatching domain event {EventType} to {HandlerType}")]
    private partial void LogDispatchingEvent(string eventType, string handlerType);

    [LoggerMessage(Level = LogLevel.Error, Message = "Domain event handler {HandlerType} failed for {EventType}")]
    private partial void LogHandlerFailed(Exception ex, string eventType, string handlerType);
}
