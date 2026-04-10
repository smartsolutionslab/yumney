using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;

public interface IShoppingEventStore
{
    /// <summary>
    /// Load the aggregate by replaying events (with optional snapshot).
    /// Returns null if no aggregate exists for this owner.
    /// </summary>
    Task<ShoppingLedger?> LoadAsync(string ownerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Append uncommitted events and optionally save a snapshot.
    /// </summary>
    Task SaveAsync(ShoppingLedger ledger, CancellationToken cancellationToken = default);
}
