using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;

public interface IShoppingListWriter
{
	Task AddItemsAsync(IReadOnlyList<ShoppingItemRequest> items, CancellationToken cancellationToken = default);
}
