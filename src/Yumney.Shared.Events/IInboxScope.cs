namespace SmartSolutionsLab.Yumney.Shared.Events;

/// <summary>
/// Per-handler invocation scope returned by <see cref="IInboxStore.BeginAsync"/>.
/// Owns the underlying transaction (if any) and decides whether the handler
/// should run. Callers must dispose the scope; on a successful handler run
/// they call <see cref="CommitAsync"/> to persist the inbox row together
/// with the handler's writes, otherwise <see cref="RollbackAsync"/> to
/// release the transaction so a retry can pick the message up again.
/// A <see cref="DisposeAsync"/> without prior commit rolls back implicitly.
/// </summary>
public interface IInboxScope : IAsyncDisposable
{
	bool ShouldProcess { get; }

	Task CommitAsync(CancellationToken cancellationToken = default);

	Task RollbackAsync(CancellationToken cancellationToken = default);

	bool IsDuplicateInboxViolation(Exception exception);
}
