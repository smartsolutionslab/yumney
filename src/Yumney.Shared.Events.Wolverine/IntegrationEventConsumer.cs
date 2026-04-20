using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shared.Events;

namespace SmartSolutionsLab.Yumney.Shared.Events.Wolverine;

/// <summary>
/// Generic Wolverine handler that delegates to IIntegrationEventHandler&lt;TEvent&gt; implementations.
/// Each handler invocation is gated by the registered <see cref="IInboxStore"/> so redelivered
/// or duplicate messages do not replay side effects.
/// </summary>
/// <typeparam name="TEvent">The integration event type to handle.</typeparam>
#pragma warning disable SA1601
public sealed partial class IntegrationEventConsumer<TEvent>(
	IServiceProvider serviceProvider,
	IInboxStore inboxStore,
	ILogger<IntegrationEventConsumer<TEvent>> logger)
	where TEvent : class, IIntegrationEvent
{
	public async Task HandleAsync(TEvent message, CancellationToken cancellationToken)
	{
		var handlers = serviceProvider.GetServices<IIntegrationEventHandler<TEvent>>();

		foreach (var handler in handlers)
		{
			var consumerName = handler.GetType().FullName ?? handler.GetType().Name;
			var shouldProcess = await inboxStore.TryMarkProcessedAsync(
				message.EventIdentifier, consumerName, cancellationToken);

			if (!shouldProcess)
			{
				LogSkippingDuplicate(typeof(TEvent).Name, consumerName, message.EventIdentifier);
				continue;
			}

			LogHandlingEvent(typeof(TEvent).Name, handler.GetType().Name);
			await handler.HandleAsync(message, cancellationToken);
		}
	}

	[LoggerMessage(Level = LogLevel.Debug, Message = "Handling integration event {EventType} with {HandlerType}")]
	private partial void LogHandlingEvent(string eventType, string handlerType);

	[LoggerMessage(Level = LogLevel.Information, Message = "Skipping duplicate integration event {EventType} for {ConsumerName} (id {MessageId})")]
	private partial void LogSkippingDuplicate(string eventType, string consumerName, Guid messageId);
}
