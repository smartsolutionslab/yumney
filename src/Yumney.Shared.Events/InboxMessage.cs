namespace SmartSolutionsLab.Yumney.Shared.Events;

/// <summary>
/// Persisted record of a (messageId, consumerName) pair that has already
/// been processed. The primary key is the composite of the two fields;
/// a duplicate insert attempt is the signal to skip the handler.
/// </summary>
public sealed class InboxMessage
{
	public required Guid MessageId { get; init; }

	public required string ConsumerName { get; init; }

	public DateTime ProcessedAt { get; init; } = DateTime.UtcNow;
}
