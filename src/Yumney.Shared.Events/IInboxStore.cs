namespace SmartSolutionsLab.Yumney.Shared.Events;

/// <summary>
/// Deduplication store for integration-event consumers. Implementations are
/// expected to persist a (messageId, consumerName) pair atomically and return
/// <c>true</c> only when the pair was not already present. Consumers that
/// receive <c>false</c> must skip the handler to avoid replaying side effects.
/// </summary>
public interface IInboxStore
{
	Task<bool> TryMarkProcessedAsync(Guid messageId, string consumerName, CancellationToken cancellationToken = default);
}
