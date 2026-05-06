using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Events;

namespace SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;

/// <summary>
/// Maps a single domain event type to a cross-module integration event.
/// Implementations live in the publishing module's infrastructure assembly
/// (so they can reference both the domain event and the cross-module event)
/// and are auto-discovered by <see cref="CrossModuleEventConvention"/>.
/// Return null from TryMap when the domain event has no integration-event
/// counterpart for the given state (e.g. conditional emission).
/// </summary>
public interface ICrossModuleEventMapper
{
	Type DomainEventType { get; }

	IIntegrationEvent? TryMap(IReadOnlyList<object> context, IDomainEvent domainEvent);
}
