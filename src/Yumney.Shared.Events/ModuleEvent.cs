namespace SmartSolutionsLab.Yumney.Shared.Events;

/// <summary>
/// Abstract record base for in-module bus envelopes. Concrete events sit in
/// the owning module's <c>Infrastructure</c> layer and wrap a Domain event as
/// their <c>Inner</c> property.
/// </summary>
public abstract record ModuleEvent(string OwnerId) : IModuleEvent
{
	// `init` is required so System.Text.Json can round-trip these values when
	// the event is rehydrated from a Wolverine envelope. Without it the
	// inbox-dedup key (EventIdentifier) is regenerated on every redelivery,
	// breaking exactly-once semantics, and OccurredOn is reset to "now".
	public Guid EventIdentifier { get; init; } = Guid.NewGuid();

	public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
