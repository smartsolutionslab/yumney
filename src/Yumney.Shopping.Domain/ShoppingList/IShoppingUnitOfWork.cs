using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

public interface IShoppingUnitOfWork : IUnitOfWork
{
	IShoppingListRepository ShoppingLists { get; }
}
