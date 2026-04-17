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
            var request = new AddItemRequest(item.ItemName, item.Quantity, item.Unit);
            await client.PostAsJsonAsync("/api/v1/shopping-lists/items", request, cancellationToken);
        }
    }

    private sealed record AddItemRequest(string Name, decimal Quantity, string? Unit);
}
