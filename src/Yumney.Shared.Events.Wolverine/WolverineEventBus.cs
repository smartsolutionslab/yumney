using Microsoft.Extensions.Logging;
using Wolverine;

namespace SmartSolutionsLab.Yumney.Shared.Events.Wolverine;

/// <summary>
/// IEventBus implementation that publishes integration events via Wolverine/RabbitMQ.
/// </summary>
public sealed partial class WolverineEventBus(IMessageBus messageBus, ILogger<WolverineEventBus> logger) : IEventBus
{
	public async Task PublishAsync<TEvent>(TEvent busEvent, CancellationToken cancellationToken = default)
		where TEvent : IBusEvent
	{
		if (logger.IsEnabled(LogLevel.Debug))
		{
			LogPublishingBusEvent(logger, typeof(TEvent).Name);
		}

		await messageBus.PublishAsync(busEvent);
	}

	[LoggerMessage(LogLevel.Debug, "Publishing bus event {EventType} via Wolverine")]
	static partial void LogPublishingBusEvent(ILogger<WolverineEventBus> logger, string EventType);
}
