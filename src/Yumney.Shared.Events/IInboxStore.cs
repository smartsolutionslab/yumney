namespace SmartSolutionsLab.Yumney.Shared.Events;

/// <summary>
/// Deduplication store for integration- and module-event consumers.
/// <para>
/// <see cref="ProcessAsync"/> wraps the entire (check + handler + record)
/// flow inside the implementation so EF Core's retry strategy can treat it
/// as a single retriable unit — a previous scope-style API conflicted with
/// <c>EnableRetryOnFailure</c> because user-initiated transactions can't
/// span multiple async hops outside the strategy's <c>ExecuteAsync</c>.
/// </para>
/// </summary>
public interface IInboxStore
{
	/// <summary>
	/// Runs <paramref name="handler"/> exactly once per (messageId, consumerName)
	/// pair, transactionally. Returns <c>true</c> if the handler ran (and the
	/// inbox row was committed), <c>false</c> if the message was already recorded
	/// and the handler was skipped. A handler exception is rethrown after the
	/// inbox row is rolled back, so a redelivery can retry the work; a duplicate-
	/// row race is caught and treated as already-processed (returns <c>false</c>).
	/// </summary>
	Task<bool> ProcessAsync(Guid messageId, string consumerName, Func<Task> handler, CancellationToken cancellationToken = default);
}
