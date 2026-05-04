using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shared.Events;

namespace SmartSolutionsLab.Yumney.Shared.Events.Wolverine;

/// <summary>
/// Generic Wolverine handler that delegates to IIntegrationEventHandler&lt;TEvent&gt; implementations.
/// Each invocation runs inside an <see cref="IInboxScope"/> so the inbox row and the handler's
/// writes share a single transaction: the row only commits on handler success, and a thrown
/// handler rolls the row back so a redelivery can retry the work.
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
			await InvokeWithInboxAsync(handler, message, consumerName, cancellationToken);
		}
	}

	private async Task InvokeWithInboxAsync(
		IIntegrationEventHandler<TEvent> handler,
		TEvent message,
		string consumerName,
		CancellationToken cancellationToken)
	{
		await using var scope = await inboxStore.BeginAsync(message.EventIdentifier, consumerName, cancellationToken);

		if (!scope.ShouldProcess)
		{
			LogSkippingDuplicate(typeof(TEvent).Name, consumerName, message.EventIdentifier);
			return;
		}

		LogHandlingEvent(typeof(TEvent).Name, handler.GetType().Name);

		try
		{
			await handler.HandleAsync(message, cancellationToken);
			await scope.CommitAsync(cancellationToken);
		}
		catch (Exception exception) when (scope.IsDuplicateInboxViolation(exception))
		{
			await scope.RollbackAsync(cancellationToken);
			LogSkippingDuplicateRace(typeof(TEvent).Name, consumerName, message.EventIdentifier);
		}
		catch (Exception exception)
		{
			await scope.RollbackAsync(cancellationToken);
			LogHandlerFailed(exception, typeof(TEvent).Name, handler.GetType().Name, message.EventIdentifier);
			throw;
		}
	}

	[LoggerMessage(Level = LogLevel.Debug, Message = "Handling integration event {EventType} with {HandlerType}")]
	private partial void LogHandlingEvent(string eventType, string handlerType);

	[LoggerMessage(Level = LogLevel.Information, Message = "Skipping duplicate integration event {EventType} for {ConsumerName} (id {MessageId})")]
	private partial void LogSkippingDuplicate(string eventType, string consumerName, Guid messageId);

	[LoggerMessage(Level = LogLevel.Information, Message = "Concurrent peer already recorded integration event {EventType} for {ConsumerName} (id {MessageId}); rolling back and skipping")]
	private partial void LogSkippingDuplicateRace(string eventType, string consumerName, Guid messageId);

	[LoggerMessage(Level = LogLevel.Error, Message = "Handler {HandlerType} for integration event {EventType} (id {MessageId}) threw — inbox row rolled back, message will be retried")]
	private partial void LogHandlerFailed(Exception exception, string eventType, string handlerType, Guid messageId);
}
