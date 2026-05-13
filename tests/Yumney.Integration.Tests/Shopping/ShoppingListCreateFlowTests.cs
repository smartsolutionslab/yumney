using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Shopping;

[Collection(AspireCollection.Name)]
public class ShoppingListCreateFlowTests(AspireFixture fixture)
{
	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

	[Fact]
	public async Task CreateAndRetrieve_PersistsShoppingList()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");

		// Create
		var createRequest = new
		{
			title = "Integration Test List",
			items = new object[]
			{
				new { name = "Pasta", amount = 500m, unit = "g" },
				new { name = "Tomatoes", amount = 4m },
				new { name = "Olive Oil", amount = 100m, unit = "ml" },
			},
		};

		var createResponse = await client.PostAsJsonAsync("/api/v1/shopping-lists", createRequest);

		createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
		var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		var identifier = created.GetProperty("identifier").GetGuid();
		identifier.Should().NotBeEmpty();

		// Retrieve via the read model — projection handler runs asynchronously
		// on the Wolverine worker, so the GET races the handler.
		await Eventually.AssertAsync(async () =>
		{
			var getResponse = await client.GetAsync($"/api/v1/shopping-lists/{identifier}");
			getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
			var detail = await getResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
			detail.GetProperty("title").GetString().Should().Be("Integration Test List");
			detail.GetProperty("items").GetArrayLength().Should().Be(3);
		});
	}

	[Fact]
	public async Task CreateWithRecipeReference_PersistsLink()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");

		var recipeId = Guid.NewGuid();
		var createRequest = new
		{
			title = "Recipe Link Test",
			items = new[] { new { name = "Flour", amount = 1000m, unit = "g" } },
			recipeIdentifier = recipeId,
		};

		var createResponse = await client.PostAsJsonAsync("/api/v1/shopping-lists", createRequest);

		createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
		var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

		created.GetProperty("recipeReference").GetGuid().Should().Be(recipeId);
	}

	[Fact]
	public async Task CreateAndCheckOff_PersistsCheckedState()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");

		// Create
		var createRequest = new
		{
			title = "Check-Off Test",
			items = new[] { new { name = "Milk", amount = 1m, unit = "L" } },
		};

		var createResponse = await client.PostAsJsonAsync("/api/v1/shopping-lists", createRequest);
		createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
		var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		var listId = created.GetProperty("identifier").GetGuid();
		var itemId = created.GetProperty("items")[0].GetProperty("identifier").GetGuid();

		// Check off item
		var checkResponse = await client.PutAsJsonAsync(
			$"/api/v1/shopping-lists/{listId}/items/{itemId}/check",
			new { isChecked = true });

		checkResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

		await Eventually.AssertAsync(async () =>
		{
			var getResponse = await client.GetAsync($"/api/v1/shopping-lists/{listId}");
			var detail = await getResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
			detail.GetProperty("items")[0].GetProperty("isChecked").GetBoolean().Should().BeTrue();
		});
	}

	[Fact]
	public async Task Create_WithoutAuth_Returns401()
	{
		var client = fixture.ShoppingApi;

		var createRequest = new
		{
			title = "Unauthorized",
			items = new[] { new { name = "Water", amount = 1m, unit = "L" } },
		};

		var response = await client.PostAsJsonAsync("/api/v1/shopping-lists", createRequest);

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task Create_EmptyTitle_ReturnsValidationError()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");

		var createRequest = new
		{
			title = string.Empty,
			items = new[] { new { name = "Milk", amount = 1m, unit = "L" } },
		};

		var response = await client.PostAsJsonAsync("/api/v1/shopping-lists", createRequest);

		response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
	}

	[Fact]
	public async Task Create_NoItems_ReturnsValidationError()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");

		var createRequest = new
		{
			title = "Empty List",
			items = Array.Empty<object>(),
		};

		var response = await client.PostAsJsonAsync("/api/v1/shopping-lists", createRequest);

		response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
	}
}
