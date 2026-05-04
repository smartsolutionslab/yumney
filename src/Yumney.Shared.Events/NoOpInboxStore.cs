namespace SmartSolutionsLab.Yumney.Shared.Events;

/// <summary>
/// Pass-through inbox that never records anything and always allows the
/// consumer to handle the message. This is the default binding and
/// preserves pre-inbox behaviour for modules that have not yet run
/// the migration needed by a persistent store.
/// </summary>
public sealed class NoOpInboxStore : IInboxStore
{
	public Task<IInboxScope> BeginAsync(Guid messageId, string consumerName, CancellationToken cancellationToken = default)
		=> Task.FromResult<IInboxScope>(NoOpInboxScope.Instance);
}

internal sealed class NoOpInboxScope : IInboxScope
{
	public static readonly NoOpInboxScope Instance = new();

	public bool ShouldProcess => true;

	public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

	public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

	public bool IsDuplicateInboxViolation(Exception exception) => false;

	public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
