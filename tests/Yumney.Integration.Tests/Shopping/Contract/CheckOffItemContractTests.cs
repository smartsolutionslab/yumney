using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Shopping.Contract;

[Collection(AspireCollection.Name)]
public class CheckOffItemContractTests(AspireFixture fixture) : IAsyncLifetime
{
	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

	public Task InitializeAsync() => Task.CompletedTask;

	public async Task DisposeAsync()
	{
		var userId = await fixture.GetTestUserIdAsync();
		var owner = OwnerIdentifier.From(userId);
		await fixture.ResetShoppingListEventStoreAsync(owner);
	}

	[Fact]
	public async Task CheckOffItem_IsCheckedTrue_Returns204AndChecksOnlyTargetItem()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");
		var (listId, items) = await CreateListAsync(client);
		var targetItemId = items[0].GetProperty("identifier").GetGuid();

		var response = await client.PutAsJsonAsync(
			$"/api/v1/shopping-lists/{listId}/items/{targetItemId}/check",
			new { isChecked = true });

		response.StatusCode.Should().Be(HttpStatusCode.NoContent);
		await Eventually.AssertAsync(async () =>
		{
			var detail = await GetListAsync(client, listId);
			var checkedItems = detail.GetProperty("items").EnumerateArray()
				.Where(entry => entry.GetProperty("isChecked").GetBoolean())
				.Select(entry => entry.GetProperty("identifier").GetGuid())
				.ToList();
			checkedItems.Should().ContainSingle().Which.Should().Be(targetItemId);
		});
	}

	[Fact]
	public async Task CheckOffItem_IsCheckedFalse_Returns204AndUnchecksTargetItem()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");
		var (listId, items) = await CreateListAsync(client);
		var targetItemId = items[0].GetProperty("identifier").GetGuid();
		await client.PutAsJsonAsync(
			$"/api/v1/shopping-lists/{listId}/items/{targetItemId}/check",
			new { isChecked = true });

		var response = await client.PutAsJsonAsync(
			$"/api/v1/shopping-lists/{listId}/items/{targetItemId}/check",
			new { isChecked = false });

		response.StatusCode.Should().Be(HttpStatusCode.NoContent);
		await Eventually.AssertAsync(async () =>
		{
			var detail = await GetListAsync(client, listId);
			detail.GetProperty("items").EnumerateArray()
				.Select(entry => entry.GetProperty("isChecked").GetBoolean())
				.Should().AllBeEquivalentTo(false);
		});
	}

	[Fact]
	public async Task CheckOffItem_AlreadyCheckedItem_ReturnsSuccessIdempotently()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");
		var (listId, items) = await CreateListAsync(client);
		var targetItemId = items[0].GetProperty("identifier").GetGuid();
		var firstResponse = await client.PutAsJsonAsync(
			$"/api/v1/shopping-lists/{listId}/items/{targetItemId}/check",
			new { isChecked = true });
		firstResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

		var secondResponse = await client.PutAsJsonAsync(
			$"/api/v1/shopping-lists/{listId}/items/{targetItemId}/check",
			new { isChecked = true });

		secondResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
	}

	[Fact]
	public async Task CheckOffItem_ListDoesNotExist_Returns404()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");

		var response = await client.PutAsJsonAsync(
			$"/api/v1/shopping-lists/{Guid.NewGuid()}/items/{Guid.NewGuid()}/check",
			new { isChecked = true });

		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task CheckOffItem_ItemDoesNotExist_Returns400()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");
		var (listId, _) = await CreateListAsync(client);

		var response = await client.PutAsJsonAsync(
			$"/api/v1/shopping-lists/{listId}/items/{Guid.NewGuid()}/check",
			new { isChecked = true });

		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task CheckOffItem_WithoutAuth_Returns401()
	{
		var client = fixture.ShoppingApi;

		var response = await client.PutAsJsonAsync(
			$"/api/v1/shopping-lists/{Guid.NewGuid()}/items/{Guid.NewGuid()}/check",
			new { isChecked = true });

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	private static async Task<(Guid ListId, JsonElement[] Items)> CreateListAsync(HttpClient client)
	{
		var response = await client.PostAsJsonAsync("/api/v1/shopping-lists/", new
		{
			title = "Check-Item Test",
			items = new object[]
			{
				new { name = "Milk", amount = 1m, unit = "l" },
				new { name = "Bread", amount = 1m },
			},
		});
		response.EnsureSuccessStatusCode();
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		var listId = body.GetProperty("identifier").GetGuid();
		var items = body.GetProperty("items").EnumerateArray().ToArray();
		return (listId, items);
	}

	private static async Task<JsonElement> GetListAsync(HttpClient client, Guid listId)
	{
		var response = await client.GetAsync($"/api/v1/shopping-lists/{listId}");
		response.EnsureSuccessStatusCode();
		return await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
	}
}
