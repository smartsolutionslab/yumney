using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shared.Events;

namespace SmartSolutionsLab.Yumney.Shared.Events.Wolverine;

/// <summary>
/// Generic Wolverine handler that delegates to IIntegrationEventHandler&lt;TEvent&gt; implementations.
/// Wolverine discovers this via convention (public Handle method).
/// </summary>
/// <typeparam name="TEvent">The integration event type to handle.</typeparam>
#pragma warning disable SA1601
public sealed partial class IntegrationEventConsumer<TEvent>(
	IServiceProvider serviceProvider,
	ILogger<IntegrationEventConsumer<TEvent>> logger)
	where TEvent : class, IIntegrationEvent
{
	public async Task HandleAsync(TEvent message, CancellationToken cancellationToken)
	{
		var handlers = serviceProvider.GetServices<IIntegrationEventHandler<TEvent>>();

		foreach (var handler in handlers)
		{
			LogHandlingEvent(typeof(TEvent).Name, handler.GetType().Name);

			await handler.HandleAsync(message, cancellationToken);
		}
	}

	[LoggerMessage(Level = LogLevel.Debug, Message = "Handling integration event {EventType} with {HandlerType}")]
	private partial void LogHandlingEvent(string eventType, string handlerType);
}
