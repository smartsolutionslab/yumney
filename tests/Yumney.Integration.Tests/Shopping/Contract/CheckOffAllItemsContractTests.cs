using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Shopping.Contract;

/// <summary>
/// Contract tests for PUT /api/v1/shopping-lists/{id}/check-all.
/// </summary>
[Collection(AspireCollection.Name)]
public class CheckOffAllItemsContractTests(AspireFixture fixture) : IAsyncLifetime
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
	public async Task CheckOffAll_IsCheckedTrue_Returns204AndMarksAllItemsChecked()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");
		var listId = await CreateListAsync(client);

		var response = await client.PutAsJsonAsync(
			$"/api/v1/shopping-lists/{listId}/check-all",
			new { isChecked = true });

		response.StatusCode.Should().Be(HttpStatusCode.NoContent);
		await Eventually.AssertAsync(async () =>
		{
			var detail = await GetListAsync(client, listId);
			detail.GetProperty("items").EnumerateArray()
				.Select(entry => entry.GetProperty("isChecked").GetBoolean())
				.Should().AllBeEquivalentTo(true);
		});
	}

	[Fact]
	public async Task CheckOffAll_IsCheckedFalse_Returns204AndUnchecksAllItems()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");
		var listId = await CreateListAsync(client);
		await client.PutAsJsonAsync($"/api/v1/shopping-lists/{listId}/check-all", new { isChecked = true });

		var response = await client.PutAsJsonAsync(
			$"/api/v1/shopping-lists/{listId}/check-all",
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
	public async Task CheckOffAll_ListDoesNotExist_Returns404()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");

		var response = await client.PutAsJsonAsync(
			$"/api/v1/shopping-lists/{Guid.NewGuid()}/check-all",
			new { isChecked = true });

		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task CheckOffAll_WithoutAuth_Returns401()
	{
		var client = fixture.ShoppingApi;

		var response = await client.PutAsJsonAsync(
			$"/api/v1/shopping-lists/{Guid.NewGuid()}/check-all",
			new { isChecked = true });

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	private static async Task<Guid> CreateListAsync(HttpClient client)
	{
		var response = await client.PostAsJsonAsync("/api/v1/shopping-lists/", new
		{
			title = "Check-All Test",
			items = new object[]
			{
				new { name = "Milk", amount = 1m, unit = "l" },
				new { name = "Bread", amount = 1m },
			},
		});
		response.EnsureSuccessStatusCode();
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		return body.GetProperty("identifier").GetGuid();
	}

	private static async Task<JsonElement> GetListAsync(HttpClient client, Guid listId)
	{
		var response = await client.GetAsync($"/api/v1/shopping-lists/{listId}");
		response.EnsureSuccessStatusCode();
		return await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
	}
}
