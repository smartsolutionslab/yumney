using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;

public interface IShoppingEventStore
{
	Task<ShoppingLedger> LoadAsync(OwnerIdentifier ownerId, CancellationToken cancellationToken = default);

	Task<ShoppingLedger?> FindAsync(OwnerIdentifier ownerId, CancellationToken cancellationToken = default);

	Task SaveAsync(ShoppingLedger ledger, CancellationToken cancellationToken = default);
}
