namespace SmartSolutionsLab.Yumney.Shared.Events;

/// <summary>
/// Pass-through inbox that runs the handler with no dedup. Default binding
/// for modules that have not yet adopted the EF Core inbox; preserves the
/// pre-inbox at-least-once behaviour where a handler exception propagates
/// to Wolverine and the message is redelivered.
/// </summary>
public sealed class NoOpInboxStore : IInboxStore
{
	public async Task<InboxOutcome> TryProcessAsync(
		Guid messageId,
		string consumerName,
		Func<CancellationToken, Task> handler,
		CancellationToken cancellationToken = default)
	{
		await handler(cancellationToken);
		return InboxOutcome.Processed;
	}
}
