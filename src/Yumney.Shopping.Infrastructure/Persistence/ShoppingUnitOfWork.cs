using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;

public sealed class ShoppingUnitOfWork(ShoppingDbContext context, IShoppingListRepository shoppingLists) : IShoppingUnitOfWork
{
	public IShoppingListRepository ShoppingLists => shoppingLists;

	public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
		=> context.SaveChangesAsync(cancellationToken);
}
