using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Recipes.Contract;

/// <summary>
/// Contract tests for GET /api/v1/recipes — paginated, owner-scoped.
/// </summary>
[Collection(AspireCollection.Name)]
public class GetRecipesContractTests(AspireFixture fixture) : IAsyncLifetime
{
	private const string Endpoint = "/api/v1/recipes";

	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

	public Task InitializeAsync() => CleanupAsync();

	public Task DisposeAsync() => CleanupAsync();

	[Fact]
	public async Task GetRecipes_NoRecipesForOwner_ReturnsEmptyPagedResult()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("recipes-api");

		var response = await client.GetAsync(Endpoint);

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		body.GetProperty("items").GetArrayLength().Should().Be(0);
		body.GetProperty("totalCount").GetInt32().Should().Be(0);
	}

	[Fact]
	public async Task GetRecipes_AfterSeedingTwo_ReturnsBoth()
	{
		var userId = await fixture.GetTestUserIdAsync();
		await fixture.SeedRecipesAsync(RecipeFactory.Lasagne(userId), RecipeFactory.TomatoSoup(userId));
		using var client = await fixture.CreateAuthenticatedClientAsync("recipes-api");

		var response = await client.GetAsync(Endpoint);

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		body.GetProperty("totalCount").GetInt32().Should().Be(2);
	}

	[Fact]
	public async Task GetRecipes_PageSize1_LimitsAndReportsTotalCount()
	{
		var userId = await fixture.GetTestUserIdAsync();
		await fixture.SeedRecipesAsync(RecipeFactory.Lasagne(userId), RecipeFactory.TomatoSoup(userId));
		using var client = await fixture.CreateAuthenticatedClientAsync("recipes-api");

		var response = await client.GetAsync($"{Endpoint}?page=1&pageSize=1");

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		body.GetProperty("items").GetArrayLength().Should().Be(1);
		body.GetProperty("totalCount").GetInt32().Should().Be(2);
	}

	[Fact]
	public async Task GetRecipes_FavoritesFilterTrue_ReturnsOnlyFavorited()
	{
		var userId = await fixture.GetTestUserIdAsync();
		await fixture.SeedRecipesAsync(RecipeFactory.Lasagne(userId), RecipeFactory.TomatoSoup(userId));
		using var client = await fixture.CreateAuthenticatedClientAsync("recipes-api");

		var response = await client.GetAsync($"{Endpoint}?favorites=true");

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		body.GetProperty("totalCount").GetInt32().Should().Be(0);
	}

	[Fact]
	public async Task GetRecipes_WithoutAuth_Returns401()
	{
		var client = fixture.RecipesApi;

		var response = await client.GetAsync(Endpoint);

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	private async Task CleanupAsync()
	{
		var userId = await fixture.GetTestUserIdAsync();
		var owner = OwnerIdentifier.From(userId);
		await AspireFixture.CleanupAsync(
			fixture.CreateRecipesDbContextAsync,
			ctx => ctx.Recipes.Where(recipe => recipe.Owner == owner));
	}
}
