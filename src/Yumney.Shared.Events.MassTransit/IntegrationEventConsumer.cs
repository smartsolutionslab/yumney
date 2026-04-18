using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shared.Events;

namespace SmartSolutionsLab.Yumney.Shared.Events.MassTransit;

/// <summary>
/// Generic MassTransit consumer that delegates to IIntegrationEventHandler&lt;TEvent&gt; implementations.
/// </summary>
/// <typeparam name="TEvent">The integration event type to consume.</typeparam>
#pragma warning disable SA1601
public sealed partial class IntegrationEventConsumer<TEvent>(
	IServiceProvider serviceProvider,
	ILogger<IntegrationEventConsumer<TEvent>> logger) : IConsumer<TEvent>
	where TEvent : class, IIntegrationEvent
{
	public async Task Consume(ConsumeContext<TEvent> context)
	{
		var handlers = serviceProvider.GetServices<IIntegrationEventHandler<TEvent>>();

		foreach (var handler in handlers)
		{
			LogHandlingEvent(typeof(TEvent).Name, handler.GetType().Name);

			await handler.HandleAsync(context.Message, context.CancellationToken);
		}
	}

	[LoggerMessage(Level = LogLevel.Debug, Message = "Handling integration event {EventType} with {HandlerType}")]
	private partial void LogHandlingEvent(string eventType, string handlerType);
}
