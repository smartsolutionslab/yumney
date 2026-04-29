using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Recipes.Contract;

/// <summary>
/// Contract tests for GET /api/v1/recipes/what-can-i-cook — exercises the
/// cross-module HTTP path Recipes → Shopping `/balance` end-to-end.
/// </summary>
[Collection(AspireCollection.Name)]
public class WhatCanICookContractTests(AspireFixture fixture) : IAsyncLifetime
{
	private const string Endpoint = "/api/v1/recipes/what-can-i-cook";

	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

	public Task InitializeAsync() => CleanupAsync();

	public Task DisposeAsync() => CleanupAsync();

	[Fact]
	public async Task WhatCanICook_NoRecipes_ReturnsEmptyArray()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("recipes-api");

		var response = await client.GetAsync(Endpoint);

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		body.GetArrayLength().Should().Be(0);
	}

	[Fact]
	public async Task WhatCanICook_RecipesButNoBalance_ReturnsRecipesAsNearMatchOrExcluded()
	{
		var userId = await fixture.GetTestUserIdAsync();

		// Lasagne has 10 ingredients — none in balance, so excluded (>2 missing).
		// TomatoSoup is a smaller recipe (4 ingredients) — also excluded.
		await fixture.SeedRecipesAsync(RecipeFactory.Lasagne(userId), RecipeFactory.TomatoSoup(userId));
		using var client = await fixture.CreateAuthenticatedClientAsync("recipes-api");

		var response = await client.GetAsync(Endpoint);

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		body.GetArrayLength().Should().Be(0);
	}

	[Fact]
	public async Task WhatCanICook_FullMatchOnly_FlagAcceptedWithoutError()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("recipes-api");

		var response = await client.GetAsync($"{Endpoint}?fullMatchOnly=true");

		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task WhatCanICook_Unauthenticated_Returns401()
	{
		using var client = fixture.RecipesApi;

		var response = await client.GetAsync(Endpoint);

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	private async Task CleanupAsync()
	{
		var userId = await fixture.GetTestUserIdAsync();
		var owner = OwnerIdentifier.From(userId);
		await AspireFixture.CleanupAsync(
			fixture.CreateRecipesDbContextAsync,
			ctx => ctx.Recipes.Where(r => r.Owner == owner));
	}
}
