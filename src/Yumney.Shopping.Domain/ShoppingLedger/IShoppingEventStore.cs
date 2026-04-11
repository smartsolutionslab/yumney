namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;

/// <summary>
/// Event store for the shopping list aggregate.
/// </summary>
public interface IShoppingEventStore
{
    /// <summary>
    /// Load the aggregate by replaying events (with optional snapshot).
    /// </summary>
    /// <param name="ownerId">The owner user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The hydrated aggregate, or null if none exists.</returns>
    Task<ShoppingLedger?> LoadAsync(string ownerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Append uncommitted events and optionally save a snapshot.
    /// </summary>
    /// <param name="ledger">The aggregate with uncommitted events.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveAsync(ShoppingLedger ledger, CancellationToken cancellationToken = default);
}
