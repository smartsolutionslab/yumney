namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

public interface IShoppingListRepository
{
    Task AddAsync(ShoppingList shoppingList, CancellationToken cancellationToken = default);

    Task<ShoppingList?> GetByIdAsync(ShoppingListIdentifier identifier, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShoppingList>> GetByOwnerAsync(OwnerIdentifier owner, CancellationToken cancellationToken = default);
}
