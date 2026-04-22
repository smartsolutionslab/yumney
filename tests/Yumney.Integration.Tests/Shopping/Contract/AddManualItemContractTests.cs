using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Shopping.Contract;

/// <summary>
/// Contract tests for POST /api/v1/shopping-lists/items.
/// Reference template for issue #279 — this is the depth/style we will scale to
/// all remaining endpoints. Every endpoint test file should probe: happy paths,
/// validator failures (422), guard failures (400 via global exception handler),
/// auth (401), and stateful/idempotent behavior where applicable.
/// </summary>
[Collection(AspireCollection.Name)]
public class AddManualItemContractTests(AspireFixture fixture) : IAsyncLifetime
{
	private const string Endpoint = "/api/v1/shopping-lists/items";

	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

	public Task InitializeAsync() => Task.CompletedTask;

	public async Task DisposeAsync()
	{
		var userId = await fixture.GetTestUserIdAsync();
		await fixture.ResetShoppingEventStoreAsync(OwnerIdentifier.From(userId));
	}

	[Fact]
	public async Task AddManualItem_NameOnly_Returns201WithResolvedDefaults()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");

		var response = await client.PostAsJsonAsync(Endpoint, new { name = "Apples" });

		response.StatusCode.Should().Be(HttpStatusCode.Created);
		response.Headers.Location!.ToString().Should().StartWith("/shopping-lists/items/");
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		body.GetProperty("itemName").GetString().Should().Be("Apples");
		body.GetProperty("quantity").GetDecimal().Should().BeGreaterThan(0m);
		body.GetProperty("source").GetString().Should().Be("manual");
		body.GetProperty("ledgerIdentifier").GetGuid().Should().NotBeEmpty();
	}

	[Fact]
	public async Task AddManualItem_WithQuantityAndUnit_Returns201WithEchoedValues()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");

		var response = await client.PostAsJsonAsync(Endpoint, new { name = "Milk", quantity = 2m, unit = "l" });

		response.StatusCode.Should().Be(HttpStatusCode.Created);
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		body.GetProperty("quantity").GetDecimal().Should().Be(2m);
		body.GetProperty("unit").GetString().Should().Be("l");
	}

	[Fact]
	public async Task AddManualItem_LeadingTrailingWhitespace_IsTrimmedBeforePersist()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");

		var response = await client.PostAsJsonAsync(Endpoint, new { name = "  Bread  " });

		response.StatusCode.Should().Be(HttpStatusCode.Created);
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		body.GetProperty("itemName").GetString().Should().Be("Bread");
	}

	[Fact]
	public async Task AddManualItem_CalledTwice_AppendsToSameLedger()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");

		var first = await client.PostAsJsonAsync(Endpoint, new { name = "Tomatoes", quantity = 3m });
		var second = await client.PostAsJsonAsync(Endpoint, new { name = "Onions", quantity = 2m });

		first.StatusCode.Should().Be(HttpStatusCode.Created);
		second.StatusCode.Should().Be(HttpStatusCode.Created);
		var firstBody = await first.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		var secondBody = await second.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		secondBody.GetProperty("ledgerIdentifier").GetGuid()
			.Should().Be(firstBody.GetProperty("ledgerIdentifier").GetGuid());
	}

	[Fact]
	public async Task AddManualItem_EmptyName_Returns422ValidationProblem()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");

		var response = await client.PostAsJsonAsync(Endpoint, new { name = string.Empty });

		response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
	}

	[Fact]
	public async Task AddManualItem_WhitespaceOnlyName_Returns422ValidationProblem()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");

		var response = await client.PostAsJsonAsync(Endpoint, new { name = "   " });

		response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
	}

	[Fact]
	public async Task AddManualItem_NameOverMaxLength_Returns400GuardFailure()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");
		var overMax = new string('x', ItemName.MaxLength + 1);

		var response = await client.PostAsJsonAsync(Endpoint, new { name = overMax });

		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task AddManualItem_NegativeQuantity_Returns400GuardFailure()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");

		var response = await client.PostAsJsonAsync(Endpoint, new { name = "Apples", quantity = -1m });

		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task AddManualItem_MissingNameField_Returns422ValidationProblem()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");

		var response = await client.PostAsJsonAsync(Endpoint, new { quantity = 5m, unit = "kg" });

		response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
	}

	[Fact]
	public async Task AddManualItem_WithoutAuth_Returns401()
	{
		var client = fixture.ShoppingApi;

		var response = await client.PostAsJsonAsync(Endpoint, new { name = "Apples" });

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}
}
