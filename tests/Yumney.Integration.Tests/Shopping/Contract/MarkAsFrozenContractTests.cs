using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Shopping.Contract;

/// <summary>
/// Contract tests for POST /api/v1/shopping-lists/items/freeze (US-341 override).
/// Verifies the route resolves through DI, validation, command handler, and
/// event store. The "freeze + observe in /balance" happy path is exercised by
/// the projection unit tests against a deterministic clock — the API surface
/// here doesn't expose a direct way to mint a Bought event, so an end-to-end
/// balance check would need significant test scaffolding for marginal extra
/// signal.
/// </summary>
[Collection(AspireCollection.Name)]
public class MarkAsFrozenContractTests(AspireFixture fixture) : IAsyncLifetime
{
	private const string FreezeEndpoint = "/api/v1/shopping-lists/items/freeze";

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
}
