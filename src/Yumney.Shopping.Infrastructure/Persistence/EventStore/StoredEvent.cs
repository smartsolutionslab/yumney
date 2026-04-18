namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

/// <summary>
/// Persisted event in the append-only event store.
/// </summary>
public sealed class StoredEvent
{
	public Guid Id { get; set; }

	public Guid AggregateId { get; set; }

	public string EventType { get; set; } = default!;

	public string EventData { get; set; } = default!;

	public int Version { get; set; }

	public DateTime OccurredAt { get; set; }
}
