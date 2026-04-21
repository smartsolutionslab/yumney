using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Shopping.Contract;

/// <summary>
/// Contract tests for DELETE /api/v1/shopping-lists/items.
/// Removal is idempotent at the domain layer — absent item / absent ledger returns 204.
/// </summary>
[Collection(AspireCollection.Name)]
public class RemoveItemContractTests(AspireFixture fixture) : IAsyncLifetime
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
	public async Task RemoveItem_ExistingItemOnLedger_Returns204()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");
		await client.PostAsJsonAsync("/api/v1/shopping-lists/items", new { name = "Bananas", quantity = 3m });

		var request = new HttpRequestMessage(HttpMethod.Delete, Endpoint)
		{
			Content = JsonContent.Create(new { name = "Bananas", quantity = 1m }, options: JsonOptions),
		};
		var response = await client.SendAsync(request);

		response.StatusCode.Should().Be(HttpStatusCode.NoContent);
	}

	[Fact]
	public async Task RemoveItem_NoLedgerForOwner_Returns204Idempotently()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");

		var request = new HttpRequestMessage(HttpMethod.Delete, Endpoint)
		{
			Content = JsonContent.Create(new { name = "Nothing", quantity = 1m }, options: JsonOptions),
		};
		var response = await client.SendAsync(request);

		response.StatusCode.Should().Be(HttpStatusCode.NoContent);
	}

	[Fact]
	public async Task RemoveItem_EmptyName_Returns422ValidationProblem()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");

		var request = new HttpRequestMessage(HttpMethod.Delete, Endpoint)
		{
			Content = JsonContent.Create(new { name = string.Empty }, options: JsonOptions),
		};
		var response = await client.SendAsync(request);

		response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
	}

	[Fact]
	public async Task RemoveItem_NameOverMaxLength_Returns400GuardFailure()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");
		var overMax = new string('x', ItemName.MaxLength + 1);

		var request = new HttpRequestMessage(HttpMethod.Delete, Endpoint)
		{
			Content = JsonContent.Create(new { name = overMax }, options: JsonOptions),
		};
		var response = await client.SendAsync(request);

		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task RemoveItem_NegativeQuantity_Returns400GuardFailure()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");

		var request = new HttpRequestMessage(HttpMethod.Delete, Endpoint)
		{
			Content = JsonContent.Create(new { name = "Bananas", quantity = -1m }, options: JsonOptions),
		};
		var response = await client.SendAsync(request);

		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task RemoveItem_WithoutAuth_Returns401()
	{
		var client = fixture.ShoppingApi;

		var request = new HttpRequestMessage(HttpMethod.Delete, Endpoint)
		{
			Content = JsonContent.Create(new { name = "Bananas" }, options: JsonOptions),
		};
		var response = await client.SendAsync(request);

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}
}
