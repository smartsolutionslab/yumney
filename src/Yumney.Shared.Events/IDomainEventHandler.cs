using System.Diagnostics.CodeAnalysis;
using Yumney.Shared.Common;

namespace Yumney.Shared.Events;

[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "EventHandler is the correct DDD convention")]
public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
