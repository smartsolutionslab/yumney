using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.ExternalServices;

#pragma warning disable SA1601
public sealed partial class HttpShoppingListWriter(IHttpClientFactory httpClientFactory, ILogger<HttpShoppingListWriter> logger)
	: IShoppingListWriter
{
#pragma warning disable SA1311
	private static readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);
#pragma warning restore SA1311

	public async Task AddItemsAsync(IReadOnlyList<ShoppingItemRequest> items, CancellationToken cancellationToken = default)
	{
		var client = httpClientFactory.CreateClient("shopping-api");

		foreach (var (itemName, quantity, unit, source) in items)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var request = new AddItemRequest(itemName, quantity, unit, source);
			var url = "/api/v1/shopping-lists/items";
			var response = await client.PostAsJsonAsync(url, request, jsonOptions, cancellationToken);

			if (!response.IsSuccessStatusCode)
			{
				LogAddItemFailed(itemName, response.StatusCode);
				response.EnsureSuccessStatusCode();
			}
		}
	}

	[LoggerMessage(Level = LogLevel.Error, Message = "Failed to add shopping-list item {ItemName}: HTTP {StatusCode}")]
	private partial void LogAddItemFailed(string itemName, HttpStatusCode statusCode);

#pragma warning disable SA1313, SA1402, SA1649
	private sealed record AddItemRequest(string Name, decimal Quantity, string? Unit, string Source);
}
