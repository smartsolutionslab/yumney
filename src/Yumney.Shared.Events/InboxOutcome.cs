namespace SmartSolutionsLab.Yumney.Shared.Events;

/// <summary>
/// Result of <see cref="IInboxStore.TryProcessAsync"/>. Lets the caller
/// (today, the Wolverine event consumer) emit the right log line without
/// inspecting transaction state itself.
/// </summary>
public enum InboxOutcome
{
	/// <summary>
	/// The handler ran and the inbox row + handler writes committed atomically.
	/// </summary>
	Processed,

	/// <summary>
	/// The (messageId, consumerName) pair was already recorded by an earlier
	/// (successful) delivery; the handler was not invoked.
	/// </summary>
	AlreadyProcessed,

	/// <summary>
	/// A concurrent peer committed the same (messageId, consumerName) pair
	/// between this consumer's pre-check and its commit. The unique
	/// constraint fired, the entire transaction (handler writes included)
	/// rolled back, and the original peer's commit is the source of truth.
	/// </summary>
	DuplicateRace,
}
