using System.Net.Http.Json;
using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Services;

public sealed class HttpShoppingListWriter(IHttpClientFactory httpClientFactory) : IShoppingListWriter
{
    public async Task AddItemsAsync(
        string ownerId,
        IReadOnlyList<ShoppingItemRequest> items,
        CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient("shopping-api");

        foreach (var item in items)
        {
            AddItemRequest request = new(item.ItemName, item.Quantity, item.Unit);
            var url = "/api/v1/shopping-lists/items";
            await client.PostAsJsonAsync(url, request, cancellationToken);
        }
    }

    private sealed record AddItemRequest(string Name, decimal Quantity, string? Unit);
}
