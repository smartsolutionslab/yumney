using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shared.Events;
using Wolverine;

namespace SmartSolutionsLab.Yumney.Shared.Events.Wolverine;

/// <summary>
/// IEventBus implementation that publishes integration events via Wolverine/RabbitMQ.
/// </summary>
public sealed partial class WolverineEventBus(IMessageBus messageBus, ILogger<WolverineEventBus> logger) : IEventBus
{
	public async Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
		where TEvent : IIntegrationEvent
	{
		if (logger.IsEnabled(LogLevel.Debug))
		{
			LogPublishingIntegrationEvent(logger, typeof(TEvent).Name);
		}

		await messageBus.PublishAsync(integrationEvent);
	}

	[LoggerMessage(LogLevel.Debug, "Publishing integration event {EventType} via Wolverine")]
	static partial void LogPublishingIntegrationEvent(ILogger<WolverineEventBus> logger, string EventType);
}
