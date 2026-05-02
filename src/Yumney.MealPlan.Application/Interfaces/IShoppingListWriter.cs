using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;

public interface IShoppingListWriter
{
	Task AddItemsAsync(
		OwnerIdentifier owner,
		IReadOnlyList<ShoppingItemRequest> items,
		CancellationToken cancellationToken = default);
}
