using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shopping.Client;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.ExternalServices;

public sealed class HttpShoppingListWriter(IShoppingClient shopping) : IShoppingListWriter
{
	public async Task AddItemsAsync(IReadOnlyList<ShoppingItemRequest> items, CancellationToken cancellationToken = default)
	{
		foreach (var (itemName, quantity, unit, source) in items)
		{
			cancellationToken.ThrowIfCancellationRequested();
			await shopping.AddItemAsync(new AddShoppingItemRequest(itemName, quantity, unit, source), cancellationToken);
		}
	}
}
