using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SmartSolutionsLab.Yumney.Shared.Events.Wolverine;

/// <summary>
/// Generic Wolverine handler that delegates to <see cref="IModuleEventHandler{TEvent}"/>
/// implementations. Counterpart to <see cref="IntegrationEventConsumer{TEvent}"/> for
/// in-module bus envelopes. Each invocation goes through
/// <see cref="IInboxStore.TryProcessAsync"/> so the handler's writes and the inbox
/// dedup row share a single transactional fate.
/// </summary>
/// <typeparam name="TEvent">The module event type to handle.</typeparam>
#pragma warning disable SA1601
public sealed partial class ModuleEventConsumer<TEvent>(
	IServiceProvider serviceProvider,
	IInboxStore inboxStore,
	ILogger<ModuleEventConsumer<TEvent>> logger)
	where TEvent : class, IModuleEvent
{
	public async Task HandleAsync(TEvent message, CancellationToken cancellationToken)
	{
		var handlers = serviceProvider.GetServices<IModuleEventHandler<TEvent>>();

		foreach (var handler in handlers)
		{
			var consumerName = handler.GetType().FullName ?? handler.GetType().Name;
			await InvokeWithInboxAsync(handler, message, consumerName, cancellationToken);
		}
	}

	private async Task InvokeWithInboxAsync(
		IModuleEventHandler<TEvent> handler,
		TEvent message,
		string consumerName,
		CancellationToken cancellationToken)
	{
		LogHandlingEvent(typeof(TEvent).Name, handler.GetType().Name);

		var eventIdentifier = message.EventIdentifier;
		InboxOutcome outcome;
		try
		{
			outcome = await inboxStore.TryProcessAsync(eventIdentifier, consumerName, ct => handler.HandleAsync(message, ct), cancellationToken);
		}
		catch (Exception exception)
		{
			LogHandlerFailed(exception, typeof(TEvent).Name, handler.GetType().Name, eventIdentifier);
			throw;
		}

		switch (outcome)
		{
			case InboxOutcome.AlreadyProcessed:
				LogSkippingDuplicate(typeof(TEvent).Name, consumerName, eventIdentifier);
				break;
			case InboxOutcome.DuplicateRace:
				LogSkippingDuplicateRace(typeof(TEvent).Name, consumerName, eventIdentifier);
				break;
		}
	}

	[LoggerMessage(Level = LogLevel.Debug, Message = "Handling module event {EventType} with {HandlerType}")]
	private partial void LogHandlingEvent(string eventType, string handlerType);

	[LoggerMessage(Level = LogLevel.Information, Message = "Skipping duplicate module event {EventType} for {ConsumerName} (id {MessageId})")]
	private partial void LogSkippingDuplicate(string eventType, string consumerName, Guid messageId);

	[LoggerMessage(Level = LogLevel.Information, Message = "Concurrent peer already recorded module event {EventType} for {ConsumerName} (id {MessageId}); rolled back local transaction")]
	private partial void LogSkippingDuplicateRace(string eventType, string consumerName, Guid messageId);

	[LoggerMessage(Level = LogLevel.Error, Message = "Handler {HandlerType} for module event {EventType} (id {MessageId}) threw — inbox transaction rolled back, message will be retried")]
	private partial void LogHandlerFailed(Exception exception, string eventType, string handlerType, Guid messageId);
}
