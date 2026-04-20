namespace SmartSolutionsLab.Yumney.Shared.Events;

/// <summary>
/// Pass-through inbox that never records anything and always allows the
/// consumer to handle the message. This is the default binding and
/// preserves pre-inbox behaviour for modules that have not yet run
/// the migration needed by a persistent store.
/// </summary>
public sealed class NoOpInboxStore : IInboxStore
{
	public Task<bool> TryMarkProcessedAsync(Guid messageId, string consumerName, CancellationToken cancellationToken = default)
		=> Task.FromResult(true);
}
