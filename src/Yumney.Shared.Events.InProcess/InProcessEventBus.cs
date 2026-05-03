using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SmartSolutionsLab.Yumney.Shared.Events;

#pragma warning disable SA1601
public sealed partial class InProcessEventBus(IServiceProvider serviceProvider, ILogger<InProcessEventBus> logger) : IEventBus
{
	public async Task PublishAsync<TEvent>(TEvent busEvent, CancellationToken cancellationToken = default)
		where TEvent : IBusEvent
	{
		// Resolve handlers by the runtime concrete type, not the compile-time
		// generic argument. Without this, callers that publish via an
		// `IBusEvent` / `IIntegrationEvent` static type (e.g. from a `... switch { ... }`
		// mapping that returns the marker interface) would inadvertently bind
		// TEvent to the interface and miss every concrete-type subscriber.
		var eventType = busEvent.GetType();
		var eventName = eventType.Name;

		// Cross-module integration events route to IIntegrationEventHandler<>;
		// in-module envelopes route to IModuleEventHandler<>. The two markers
		// are mutually exclusive on a concrete type, so at most one of these
		// lookups returns subscribers — the other is a cheap empty enumerable.
		if (busEvent is IIntegrationEvent)
		{
			await DispatchAsync(typeof(IIntegrationEventHandler<>), busEvent, eventType, eventName, cancellationToken);
		}

		if (busEvent is IModuleEvent)
		{
			await DispatchAsync(typeof(IModuleEventHandler<>), busEvent, eventType, eventName, cancellationToken);
		}
	}

	private async Task DispatchAsync<TEvent>(
		Type openHandlerInterface,
		TEvent busEvent,
		Type eventType,
		string eventName,
		CancellationToken cancellationToken)
		where TEvent : IBusEvent
	{
		var handlerInterface = openHandlerInterface.MakeGenericType(eventType);
		var handleMethod = handlerInterface.GetMethod("HandleAsync")!;
		var handlers = serviceProvider.GetServices(handlerInterface);

		foreach (var handler in handlers)
		{
			if (handler is null) continue;

			var handlerName = handler.GetType().Name;
			using var activity = EventsDiagnostics.ActivitySource.StartActivity($"integration_event.{eventName}.{handlerName}");
			activity?.SetTag("event.name", eventName);
			activity?.SetTag("handler.name", handlerName);

			LogPublishingEvent(eventName, handlerName);
			var start = Stopwatch.GetTimestamp();

			try
			{
				await (Task)handleMethod.Invoke(handler, [busEvent, cancellationToken])!;
				var elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
				activity?.SetTag("event.result", "success");
				activity?.SetStatus(ActivityStatusCode.Ok);
				LogHandlerCompleted(eventName, handlerName, elapsed);
			}
			catch (System.Reflection.TargetInvocationException tie) when (tie.InnerException is OperationCanceledException && cancellationToken.IsCancellationRequested)
			{
				throw tie.InnerException;
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				throw;
			}
			catch (System.Reflection.TargetInvocationException tie)
			{
				var elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
				activity?.SetTag("event.result", "error");
				activity?.SetStatus(ActivityStatusCode.Error, tie.InnerException?.Message ?? tie.Message);

				// Isolate handlers — one failing subscriber must not block the others.
				LogHandlerFailed(tie.InnerException ?? tie, eventName, handlerName, elapsed);
			}
			catch (Exception ex)
			{
				var elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
				activity?.SetTag("event.result", "error");
				activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

				// Isolate handlers — one failing subscriber must not block the others.
				LogHandlerFailed(ex, eventName, handlerName, elapsed);
			}
		}
	}

	[LoggerMessage(Level = LogLevel.Debug, Message = "Publishing integration event {EventType} to {HandlerType}")]
	private partial void LogPublishingEvent(string eventType, string handlerType);

	[LoggerMessage(Level = LogLevel.Debug, Message = "Handled integration event {EventType} in {HandlerType} in {ElapsedMs:F1}ms")]
	private partial void LogHandlerCompleted(string eventType, string handlerType, double elapsedMs);

	[LoggerMessage(Level = LogLevel.Error, Message = "Integration event handler {HandlerType} failed for {EventType} after {ElapsedMs:F1}ms")]
	private partial void LogHandlerFailed(Exception ex, string eventType, string handlerType, double elapsedMs);
}
