using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Shopping.Contract;

/// <summary>
/// Contract tests for GET /api/v1/shopping-lists/export.
/// </summary>
[Collection(AspireCollection.Name)]
public class ExportShoppingListContractTests(AspireFixture fixture) : IAsyncLifetime
{
	private const string Endpoint = "/api/v1/shopping-lists/export";

	public Task InitializeAsync() => Task.CompletedTask;

	public async Task DisposeAsync()
	{
		var userId = await fixture.GetTestUserIdAsync();
		await fixture.ResetShoppingEventStoreAsync(OwnerIdentifier.From(userId));
		await fixture.ResetShoppingReadModelAsync(userId);
	}

	[Fact]
	public async Task Export_NoItems_ReturnsOkWithTextPlainContentType()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");

		var response = await client.GetAsync(Endpoint);

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		response.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");
	}

	[Fact]
	public async Task Export_AfterAddManualItem_IncludesItemNameInPayload()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");
		await client.PostAsJsonAsync(
			"/api/v1/shopping-lists/items",
			new { name = "Strawberries", quantity = 500m, unit = "g" });

		// The shopping read-model projection is driven async by Wolverine, so
		// the export may not see the new item immediately after the POST returns.
		// Eventually applies CI-aware timeout + jitter; the bare 15s loop here
		// flaked on cold runners (#606).
		string body = string.Empty;
		HttpResponseMessage? response = null;
		try
		{
			await Eventually.AssertAsync(async () =>
			{
				response?.Dispose();
				response = await client.GetAsync(Endpoint);
				body = await response.Content.ReadAsStringAsync();
				response.StatusCode.Should().Be(HttpStatusCode.OK);
				body.Should().Contain("Strawberries");
			});
		}
		finally
		{
			response?.Dispose();
		}
	}

	[Fact]
	public async Task Export_WithoutAuth_Returns401()
	{
		var client = fixture.ShoppingApi;

		var response = await client.GetAsync(Endpoint);

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}
}
