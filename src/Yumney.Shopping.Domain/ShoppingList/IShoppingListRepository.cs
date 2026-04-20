using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

public interface IShoppingListRepository
{
	Task AddAsync(ShoppingList shoppingList, CancellationToken cancellationToken = default);

	// Throws EntityNotFoundException if not found.
	Task<ShoppingList> GetByIdAsync(ShoppingListIdentifier identifier, CancellationToken cancellationToken = default);

	// Throws EntityNotFoundException if not found.
	Task<ShoppingList> GetByIdForUpdateAsync(ShoppingListIdentifier identifier, CancellationToken cancellationToken = default);

	Task<(IReadOnlyList<ShoppingListSummary> Items, ItemCount TotalCount)> GetByOwnerAsync(
		OwnerIdentifier owner,
		PagingOptions paging,
		SortingOptions<ShoppingListSortField> sorting,
		CancellationToken cancellationToken = default);
}
