using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;

public interface IShoppingLedgerRepository
{
    Task<ShoppingLedger?> FindByOwnerAsync(OwnerIdentifier owner, CancellationToken cancellationToken = default);

    // Tracked fetch for update flows. Throws EntityNotFoundException if not found.
    Task<ShoppingLedger> GetByOwnerAsync(OwnerIdentifier owner, CancellationToken cancellationToken = default);

    Task AddAsync(ShoppingLedger ledger, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
