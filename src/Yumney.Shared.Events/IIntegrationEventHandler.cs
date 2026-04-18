using System.Diagnostics.CodeAnalysis;

namespace SmartSolutionsLab.Yumney.Shared.Events;

[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "EventHandler is the correct DDD convention")]
public interface IIntegrationEventHandler<in TEvent>
	where TEvent : IIntegrationEvent
{
	Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken = default);
}
