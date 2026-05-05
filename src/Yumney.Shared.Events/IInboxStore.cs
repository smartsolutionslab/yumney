namespace SmartSolutionsLab.Yumney.Shared.Events;

/// <summary>
/// Deduplication gate for integration- and module-event consumers.
/// Implementations run <paramref name="handler"/> only when the
/// (messageId, consumerName) pair has not been processed before, and
/// commit the dedup row in the same transactional unit as any writes the
/// handler performs through the underlying <c>DbContext</c>.
/// <para>
/// The contract is delegate-shaped — rather than returning a transaction
/// handle to the caller — because EF Core's retrying execution strategies
/// (e.g. Npgsql's <c>EnableRetryOnFailure</c>) can only retry transactions
/// when the whole begin–work–commit sequence is owned by the strategy's
/// delegate. Returning a scope to the caller and committing later, as the
/// previous design did, fails at runtime with
/// <c>InvalidOperationException: The configured execution strategy ...
/// does not support user-initiated transactions</c>.
/// </para>
/// </summary>
public interface IInboxStore
{
	Task<InboxOutcome> TryProcessAsync(
		Guid messageId,
		string consumerName,
		Func<CancellationToken, Task> handler,
		CancellationToken cancellationToken = default);
}
