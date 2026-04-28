namespace SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;

/// <summary>
/// Thrown when an event-store save conflicts with a concurrent writer
/// on the same aggregate stream — typically detected by the unique
/// (AggregateId, Version) constraint. Callers should reload the
/// aggregate, replay the command, and retry, or surface a 409 to the
/// API caller.
/// </summary>
public sealed class ConcurrencyConflictException(string aggregateName, Guid aggregateId, Exception? innerException = null)
	: Exception($"Concurrent update detected on {aggregateName} {aggregateId}.", innerException)
{
	public string AggregateName { get; } = aggregateName;

	public Guid AggregateId { get; } = aggregateId;
}
