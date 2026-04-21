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

		var response = await client.GetAsync(Endpoint);

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var body = await response.Content.ReadAsStringAsync();
		body.Should().Contain("Strawberries");
	}

	[Fact]
	public async Task Export_WithoutAuth_Returns401()
	{
		var client = fixture.ShoppingApi;

		var response = await client.GetAsync(Endpoint);

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}
}
