namespace SmartSolutionsLab.Yumney.Shared.Abstractions;

public abstract record DomainEvent : IDomainEvent
{
	// `init` is required so System.Text.Json can round-trip the value when an
	// event is rehydrated from the event store or a Wolverine envelope. Without
	// it, deserialization silently skips this read-only property and the
	// default initializer fires again, replacing the stored timestamp with
	// "now" — which corrupts replay and any consumer that reads OccurredOn.
	public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
