using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Domain.RecipeFavorite;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Recipes.Contract;

/// <summary>
/// Contract tests for GET /api/v1/recipes/{id}. Mirrors the e2e flow that
/// surfaced #427: toggle a favorite, then immediately read the recipe back
/// and confirm the per-user isFavorite flag tracks the toggle.
/// </summary>
[Collection(AspireCollection.Name)]
public class GetRecipeByIdContractTests(AspireFixture fixture) : IAsyncLifetime
{
	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

	public Task InitializeAsync() => CleanupAsync();

	public Task DisposeAsync() => CleanupAsync();

	[Fact]
	public async Task GetRecipeById_AfterToggleFavorite_ReturnsIsFavoriteTrue()
	{
		var userId = await fixture.GetTestUserIdAsync();
		var recipe = RecipeFactory.Lasagne(userId);
		await fixture.SeedRecipesAsync(recipe);
		using var client = await fixture.CreateAuthenticatedClientAsync("recipes-api");

		var toggle = await client.PostAsync($"/api/v1/recipes/{recipe.Id.Value}/favorite", content: null);
		toggle.StatusCode.Should().Be(HttpStatusCode.OK);

		var response = await client.GetAsync($"/api/v1/recipes/{recipe.Id.Value}");

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		body.GetProperty("isFavorite").GetBoolean().Should().BeTrue();
	}

	[Fact]
	public async Task GetRecipeById_AfterToggleViaSeparateClient_ReturnsIsFavoriteTrue()
	{
		var userId = await fixture.GetTestUserIdAsync();
		var recipe = RecipeFactory.Lasagne(userId);
		await fixture.SeedRecipesAsync(recipe);

		using (var toggleClient = await fixture.CreateAuthenticatedClientAsync("recipes-api"))
		{
			var toggle = await toggleClient.PostAsync($"/api/v1/recipes/{recipe.Id.Value}/favorite", content: null);
			toggle.StatusCode.Should().Be(HttpStatusCode.OK);
		}

		using var readClient = await fixture.CreateAuthenticatedClientAsync("recipes-api");
		var response = await readClient.GetAsync($"/api/v1/recipes/{recipe.Id.Value}");

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		body.GetProperty("isFavorite").GetBoolean().Should().BeTrue();
	}

	[Fact]
	public async Task GetRecipeById_NeverFavorited_ReturnsIsFavoriteFalse()
	{
		var userId = await fixture.GetTestUserIdAsync();
		var recipe = RecipeFactory.Lasagne(userId);
		await fixture.SeedRecipesAsync(recipe);
		using var client = await fixture.CreateAuthenticatedClientAsync("recipes-api");

		var response = await client.GetAsync($"/api/v1/recipes/{recipe.Id.Value}");

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		body.GetProperty("isFavorite").GetBoolean().Should().BeFalse();
	}

	[Fact]
	public async Task GetRecipeById_AfterToggleOnAndOff_ReturnsIsFavoriteFalse()
	{
		var userId = await fixture.GetTestUserIdAsync();
		var recipe = RecipeFactory.Lasagne(userId);
		await fixture.SeedRecipesAsync(recipe);
		using var client = await fixture.CreateAuthenticatedClientAsync("recipes-api");

		await client.PostAsync($"/api/v1/recipes/{recipe.Id.Value}/favorite", content: null);
		await client.PostAsync($"/api/v1/recipes/{recipe.Id.Value}/favorite", content: null);

		var response = await client.GetAsync($"/api/v1/recipes/{recipe.Id.Value}");

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		body.GetProperty("isFavorite").GetBoolean().Should().BeFalse();
	}

	private async Task CleanupAsync()
	{
		var userId = await fixture.GetTestUserIdAsync();
		var owner = OwnerIdentifier.From(userId);
		await AspireFixture.CleanupAsync(
			fixture.CreateRecipesDbContextAsync,
			ctx => ctx.Recipes.Where(recipe => recipe.Owner == owner));
		await AspireFixture.CleanupAsync(
			fixture.CreateRecipesDbContextAsync,
			ctx => ctx.RecipeFavorites.Where(favorite => favorite.Owner == owner));
	}
}
