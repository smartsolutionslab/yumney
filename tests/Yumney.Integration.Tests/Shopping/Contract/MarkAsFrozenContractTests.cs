using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Shopping.Contract;

/// <summary>
/// Contract tests for POST /api/v1/shopping-lists/items/freeze (US-341 override).
/// Drives the endpoint end-to-end through DI, validation, command handler,
/// event store publish, and projection update — guards against wiring bugs that
/// per-layer unit tests can't see.
/// </summary>
[Collection(AspireCollection.Name)]
public class MarkAsFrozenContractTests(AspireFixture fixture) : IAsyncLifetime
{
	private const string FreezeEndpoint = "/api/v1/shopping-lists/items/freeze";
	private const string BalanceEndpoint = "/api/v1/shopping-lists/balance";
	private const string ItemsEndpoint = "/api/v1/shopping-lists/items";
	private const string ShoppingModeEndEndpoint = "/api/v1/shopping-lists/shopping-mode/end";
	private const string ShoppingModeStartEndpoint = "/api/v1/shopping-lists/shopping-mode/start";

	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

	public Task InitializeAsync() => Task.CompletedTask;

	public async Task DisposeAsync()
	{
		var userId = await fixture.GetTestUserIdAsync();
		await fixture.ResetShoppingEventStoreAsync(OwnerIdentifier.From(userId));
		await fixture.ResetShoppingReadModelAsync(userId);
	}

	[Fact]
	public async Task Freeze_EmptyName_Returns422ValidationProblem()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");

		var response = await client.PostAsJsonAsync(FreezeEndpoint, new { name = string.Empty });

		response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
	}

	[Fact]
	public async Task Freeze_NoLedger_Returns204()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");

		var response = await client.PostAsJsonAsync(FreezeEndpoint, new { name = "Chicken", unit = "g" });

		response.StatusCode.Should().Be(HttpStatusCode.NoContent);
	}

	[Fact]
	public async Task Freeze_AfterBuyingItem_FlipsCategoryToFrozenInBalance()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");

		// Get an item bought by going through manual-add → shopping mode end.
		await client.PostAsJsonAsync(ItemsEndpoint, new { name = "Chicken", quantity = 500m, unit = "g" });
		await client.PostAsync(ShoppingModeStartEndpoint, content: null);
		await client.PostAsJsonAsync(ShoppingModeEndEndpoint, new { acceptPendingChanges = true });

		var freeze = await client.PostAsJsonAsync(FreezeEndpoint, new { name = "Chicken", unit = "g" });

		freeze.StatusCode.Should().Be(HttpStatusCode.NoContent);

		// Projection is async via Wolverine — poll a few times.
		var deadline = DateTime.UtcNow.AddSeconds(15);
		string? category = null;
		while (DateTime.UtcNow < deadline)
		{
			var balance = await client.GetFromJsonAsync<JsonElement>(BalanceEndpoint, JsonOptions);
			var chicken = balance.GetProperty("items").EnumerateArray()
				.FirstOrDefault(i => string.Equals(i.GetProperty("itemName").GetString(), "Chicken", StringComparison.OrdinalIgnoreCase));
			if (chicken.ValueKind == JsonValueKind.Object)
			{
				category = chicken.GetProperty("category").GetString();
				if (category == "frozen") break;
			}

			await Task.Delay(500);
		}

		category.Should().Be("frozen");
	}
}
