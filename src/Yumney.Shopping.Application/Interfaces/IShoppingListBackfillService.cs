namespace SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;

/// <summary>
/// One-off operational tool: walks the legacy <c>ShoppingLists</c> table and
/// emits synthetic events for every row that doesn't yet have a corresponding
/// entry in the event store. Lets the event-sourced read path catch up to
/// data that pre-dates Phase 2.
/// </summary>
public interface IShoppingListBackfillService
{
	/// <summary>
	/// Synthesises and persists events for legacy lists that aren't yet in the
	/// event store. Idempotent — a list with an existing
	/// <see cref="SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore.ShoppingListAggregateMetadata"/>
	/// row is skipped.
	/// </summary>
	/// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
	/// <returns>Number of legacy lists that were backfilled this run.</returns>
	Task<int> BackfillAsync(CancellationToken cancellationToken = default);
}
