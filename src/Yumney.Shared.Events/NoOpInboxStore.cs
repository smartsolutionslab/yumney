namespace SmartSolutionsLab.Yumney.Shared.Events;

/// <summary>
/// Pass-through inbox that never records anything and always invokes the
/// handler. This is the default binding and preserves pre-inbox behaviour
/// for modules that haven't activated the EF Core store.
/// </summary>
public sealed class NoOpInboxStore : IInboxStore
{
	public async Task<bool> ProcessAsync(
		Guid messageId,
		string consumerName,
		Func<Task> handler,
		CancellationToken cancellationToken = default)
	{
		await handler();
		return true;
	}
}
