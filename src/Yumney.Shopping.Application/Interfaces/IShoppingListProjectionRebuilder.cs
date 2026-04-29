namespace SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;

/// <summary>
/// Operational tool that truncates the ShoppingList projection tables and
/// re-applies every event from the event store. Used by the dashboard reset
/// entry to recover from projection drift, schema migrations, or projection
/// handler bugs without losing the source-of-truth event stream.
/// </summary>
public interface IShoppingListProjectionRebuilder
{
	/// <summary>
	/// Truncates the projection tables and replays the entire event history.
	/// Idempotent — running twice yields the same final state because the
	/// projection upserts.
	/// </summary>
	/// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
	/// <returns>The number of events replayed.</returns>
	Task<int> RebuildAsync(CancellationToken cancellationToken = default);
}
