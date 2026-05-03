using System.Diagnostics.CodeAnalysis;

namespace SmartSolutionsLab.Yumney.Shared.Events;

[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "EventHandler is the correct DDD convention")]
public interface IModuleEventHandler<in TEvent>
	where TEvent : IModuleEvent
{
	Task HandleAsync(TEvent moduleEvent, CancellationToken cancellationToken = default);
}
