using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Shopping.Contract;

/// <summary>
/// Contract tests for GET /api/v1/shopping-lists/merged.
/// Read-model projection is populated synchronously by in-process integration
/// event handlers, so items added via POST /items are visible immediately.
/// </summary>
[Collection(AspireCollection.Name)]
public class GetMergedShoppingListContractTests(AspireFixture fixture) : IAsyncLifetime
{
	private const string Endpoint = "/api/v1/shopping-lists/merged";

	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

	public Task InitializeAsync() => Task.CompletedTask;

	public async Task DisposeAsync()
	{
		var userId = await fixture.GetTestUserIdAsync();
		await fixture.ResetShoppingEventStoreAsync(OwnerIdentifier.From(userId));
		await fixture.ResetShoppingReadModelAsync(userId);
	}

	[Fact]
	public async Task GetMerged_NoItems_ReturnsEmptyDto()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");

		var response = await client.GetAsync(Endpoint);

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		body.GetProperty("items").GetArrayLength().Should().Be(0);
	}

	[Fact]
	public async Task GetMerged_AfterAddManualItem_ItemAppearsInMergedList()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");
		await client.PostAsJsonAsync("/api/v1/shopping-lists/items", new { name = "Oranges", quantity = 4m });

		// Shopping read-model projection is driven async by Wolverine; poll.
		var deadline = DateTime.UtcNow.AddSeconds(15);
		JsonElement body = default;
		HttpResponseMessage? response = null;
		while (DateTime.UtcNow < deadline)
		{
			response?.Dispose();
			response = await client.GetAsync(Endpoint);
			body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
			if (body.GetProperty("items").GetArrayLength() > 0) break;
			await Task.Delay(250);
		}

		response!.StatusCode.Should().Be(HttpStatusCode.OK);
		var items = body.GetProperty("items");
		items.GetArrayLength().Should().Be(1);
		items[0].GetProperty("itemName").GetString().Should().Be("Oranges");
		response.Dispose();
	}

	[Fact]
	public async Task GetMerged_WithoutAuth_Returns401()
	{
		var client = fixture.ShoppingApi;

		var response = await client.GetAsync(Endpoint);

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}
}
