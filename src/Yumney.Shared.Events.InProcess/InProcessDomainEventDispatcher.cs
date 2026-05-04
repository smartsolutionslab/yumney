using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Shared.Events;

#pragma warning disable SA1601
#pragma warning disable SA1311
public sealed partial class InProcessDomainEventDispatcher(IServiceProvider serviceProvider, ILogger<InProcessDomainEventDispatcher> logger)
	: IDomainEventDispatcher
{
	private static readonly ConcurrentDictionary<Type, (Type HandlerType, MethodInfo Method)> handlerCache = new();

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
				using var activity = EventsDiagnostics.ActivitySource.StartActivity($"domain_event.{eventType.Name}.{handlerName}");
				activity?.SetTag("event.name", eventType.Name);
				activity?.SetTag("handler.name", handlerName);

				LogDispatchingEvent(eventType.Name, handlerName);
				var start = Stopwatch.GetTimestamp();

				try
				{
					await (Task)method.Invoke(handler!, [domainEvent, cancellationToken])!;
					var elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
					activity?.SetTag("event.result", "success");
					activity?.SetStatus(ActivityStatusCode.Ok);
					LogHandlerCompleted(eventType.Name, handlerName, elapsed);
				}
				catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
				{
					throw;
				}
				catch (Exception ex)
				{
					var elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
					activity?.SetTag("event.result", "error");
					activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

					// Isolate handlers — one failing handler must not block the others.
					LogHandlerFailed(ex, eventType.Name, handlerName, elapsed);
				}
			}
		}
	}

	[LoggerMessage(Level = LogLevel.Debug, Message = "Dispatching domain event {EventType} to {HandlerType}")]
	private partial void LogDispatchingEvent(string eventType, string handlerType);

	[LoggerMessage(Level = LogLevel.Debug, Message = "Handled domain event {EventType} in {HandlerType} in {ElapsedMs:F1}ms")]
	private partial void LogHandlerCompleted(string eventType, string handlerType, double elapsedMs);

	[LoggerMessage(Level = LogLevel.Error, Message = "Domain event handler {HandlerType} failed for {EventType} after {ElapsedMs:F1}ms")]
	private partial void LogHandlerFailed(Exception ex, string eventType, string handlerType, double elapsedMs);
}
