using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Shopping.Contract;

/// <summary>
/// Contract tests for GET /api/v1/shopping-lists — paginated, owner-scoped.
/// </summary>
[Collection(AspireCollection.Name)]
public class GetShoppingListsContractTests(AspireFixture fixture) : IAsyncLifetime
{
	private const string Endpoint = "/api/v1/shopping-lists";

	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

	public Task InitializeAsync() => CleanupOwnersListsAsync();

	public Task DisposeAsync() => CleanupOwnersListsAsync();

	[Fact]
	public async Task GetShoppingLists_NoListsForOwner_ReturnsEmptyPagedResult()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");

		var response = await client.GetAsync(Endpoint);

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		body.GetProperty("items").GetArrayLength().Should().Be(0);
		body.GetProperty("totalCount").GetInt32().Should().Be(0);
	}

	[Fact]
	public async Task GetShoppingLists_AfterCreate_ReturnsCreatedList()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");
		await CreateListAsync(client, "List A");

		await Eventually.AssertAsync(async () =>
		{
			var response = await client.GetAsync(Endpoint);
			response.StatusCode.Should().Be(HttpStatusCode.OK);
			var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
			body.GetProperty("totalCount").GetInt32().Should().Be(1);
			body.GetProperty("items")[0].GetProperty("title").GetString().Should().Be("List A");
		});
	}

	[Fact]
	public async Task GetShoppingLists_Pagination_LimitsItemsAndReportsTotalCount()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");
		await CreateListAsync(client, "One");
		await CreateListAsync(client, "Two");
		await CreateListAsync(client, "Three");

		await Eventually.AssertAsync(async () =>
		{
			var response = await client.GetAsync($"{Endpoint}?page=1&pageSize=2");
			response.StatusCode.Should().Be(HttpStatusCode.OK);
			var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
			body.GetProperty("items").GetArrayLength().Should().Be(2);
			body.GetProperty("totalCount").GetInt32().Should().Be(3);
		});
	}

	[Fact]
	public async Task GetShoppingLists_SortByTitleAscending_ReturnsSorted()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");
		await CreateListAsync(client, "Charlie");
		await CreateListAsync(client, "Alpha");
		await CreateListAsync(client, "Bravo");

		await Eventually.AssertAsync(async () =>
		{
			var response = await client.GetAsync($"{Endpoint}?sortBy=Title&sortDirection=Ascending");
			response.StatusCode.Should().Be(HttpStatusCode.OK);
			var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
			var titles = body.GetProperty("items").EnumerateArray()
				.Select(i => i.GetProperty("title").GetString()).ToList();
			titles.Should().Equal("Alpha", "Bravo", "Charlie");
		});
	}

	[Fact]
	public async Task GetShoppingLists_WithoutAuth_Returns401()
	{
		var client = fixture.ShoppingApi;

		var response = await client.GetAsync(Endpoint);

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	private static Task<HttpResponseMessage> CreateListAsync(HttpClient client, string title) =>
		client.PostAsJsonAsync(Endpoint + "/", new
		{
			title,
			items = new[] { new { name = "Milk", amount = 1m, unit = "l" } },
		});

	private async Task CleanupOwnersListsAsync()
	{
		var userId = await fixture.GetTestUserIdAsync();
		var owner = OwnerIdentifier.From(userId);
		await fixture.ResetShoppingListEventStoreAsync(owner);
	}
}
