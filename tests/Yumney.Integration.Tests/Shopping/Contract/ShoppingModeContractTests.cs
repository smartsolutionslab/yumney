using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Shopping.Contract;

/// <summary>
/// Contract tests for the shopping-mode session endpoints
/// (POST /shopping-lists/shopping-mode/start and /end). Both are idempotent
/// at the domain layer: start while already in mode is a no-op; end while
/// not in mode is a no-op.
/// </summary>
[Collection(AspireCollection.Name)]
public class ShoppingModeContractTests(AspireFixture fixture) : IAsyncLifetime
{
	private const string StartEndpoint = "/api/v1/shopping-lists/shopping-mode/start";
	private const string EndEndpoint = "/api/v1/shopping-lists/shopping-mode/end";

	public Task InitializeAsync() => Task.CompletedTask;

	public async Task DisposeAsync()
	{
		var userId = await fixture.GetTestUserIdAsync();
		await fixture.ResetShoppingEventStoreAsync(OwnerIdentifier.From(userId));
	}

	[Fact]
	public async Task StartShoppingMode_FreshOwner_Returns204()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");

		var response = await client.PostAsync(StartEndpoint, content: null);

		response.StatusCode.Should().Be(HttpStatusCode.NoContent);
	}

	[Fact]
	public async Task StartShoppingMode_CalledTwice_IsIdempotentReturns204()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");

		var first = await client.PostAsync(StartEndpoint, content: null);
		var second = await client.PostAsync(StartEndpoint, content: null);

		first.StatusCode.Should().Be(HttpStatusCode.NoContent);
		second.StatusCode.Should().Be(HttpStatusCode.NoContent);
	}

	[Fact]
	public async Task EndShoppingMode_WithoutStart_Returns204Idempotently()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");

		var response = await client.PostAsJsonAsync(EndEndpoint, new { acceptPendingChanges = false });

		response.StatusCode.Should().Be(HttpStatusCode.NoContent);
	}

	[Fact]
	public async Task StartThenEnd_AcceptPendingChanges_Returns204()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");
		await client.PostAsync(StartEndpoint, content: null);

		var response = await client.PostAsJsonAsync(EndEndpoint, new { acceptPendingChanges = true });

		response.StatusCode.Should().Be(HttpStatusCode.NoContent);
	}

	[Fact]
	public async Task StartShoppingMode_WithoutAuth_Returns401()
	{
		var client = fixture.ShoppingApi;

		var response = await client.PostAsync(StartEndpoint, content: null);

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task EndShoppingMode_WithoutAuth_Returns401()
	{
		var client = fixture.ShoppingApi;

		var response = await client.PostAsJsonAsync(EndEndpoint, new { acceptPendingChanges = false });

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}
}
