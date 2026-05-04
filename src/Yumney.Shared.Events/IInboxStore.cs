namespace SmartSolutionsLab.Yumney.Shared.Events;

/// <summary>
/// Deduplication store for integration- and module-event consumers.
/// Implementations open a scope that gates the handler invocation: if the
/// (messageId, consumerName) row is already present the scope reports
/// <see cref="IInboxScope.ShouldProcess"/> = <c>false</c>; otherwise the
/// scope stages the row so it commits atomically with the handler's own
/// writes when <see cref="IInboxScope.CommitAsync"/> is called.
/// </summary>
public interface IInboxStore
{
	Task<IInboxScope> BeginAsync(Guid messageId, string consumerName, CancellationToken cancellationToken = default);
}
