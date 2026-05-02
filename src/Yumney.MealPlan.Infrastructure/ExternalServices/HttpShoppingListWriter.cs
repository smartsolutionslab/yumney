using System.Net.Http.Json;
using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.ExternalServices;

public sealed class HttpShoppingListWriter(IHttpClientFactory httpClientFactory) : IShoppingListWriter
{
	public async Task AddItemsAsync(
		OwnerIdentifier owner,
		IReadOnlyList<ShoppingItemRequest> items,
		CancellationToken cancellationToken = default)
	{
		var client = httpClientFactory.CreateClient("shopping-api");

		await foreach (var (itemName, quantity, unit, source) in items.ToAsyncEnumerable().WithCancellation(cancellationToken))
		{
			AddItemRequest request = new(itemName, quantity, unit, source);
			await client.PostAsJsonAsync("/api/v1/shopping-lists/items", request, cancellationToken);
		}
	}

	private sealed record AddItemRequest(string Name, decimal Quantity, string? Unit, string Source);
}
