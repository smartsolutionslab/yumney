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
/// Contract tests for POST /api/v1/recipes/{id}/favorite.
/// Toggles favorite state for the current user; returns the new state.
/// </summary>
[Collection(AspireCollection.Name)]
public class ToggleFavoriteContractTests(AspireFixture fixture) : IAsyncLifetime
{
	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

	public Task InitializeAsync() => CleanupAsync();

	public Task DisposeAsync() => CleanupAsync();

	[Fact]
	public async Task ToggleFavorite_FirstCall_Returns200AndIsFavoritedTrue()
	{
		var userId = await fixture.GetTestUserIdAsync();
		var recipe = RecipeFactory.Lasagne(userId);
		await fixture.SeedRecipesAsync(recipe);
		using var client = await fixture.CreateAuthenticatedClientAsync("recipes-api");

		var response = await client.PostAsync($"/api/v1/recipes/{recipe.Id.Value}/favorite", content: null);

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		body.GetProperty("isFavorite").GetBoolean().Should().BeTrue();
	}

	[Fact]
	public async Task ToggleFavorite_SecondCall_Returns200AndIsFavoritedFalse()
	{
		var userId = await fixture.GetTestUserIdAsync();
		var recipe = RecipeFactory.Lasagne(userId);
		await fixture.SeedRecipesAsync(recipe);
		using var client = await fixture.CreateAuthenticatedClientAsync("recipes-api");
		await client.PostAsync($"/api/v1/recipes/{recipe.Id.Value}/favorite", content: null);

		var response = await client.PostAsync($"/api/v1/recipes/{recipe.Id.Value}/favorite", content: null);

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		body.GetProperty("isFavorite").GetBoolean().Should().BeFalse();
	}

	[Fact]
	public async Task ToggleFavorite_NonExistentRecipe_Returns404()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("recipes-api");

		var response = await client.PostAsync($"/api/v1/recipes/{Guid.NewGuid()}/favorite", content: null);

		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task ToggleFavorite_WithoutAuth_Returns401()
	{
		var client = fixture.RecipesApi;

		var response = await client.PostAsync($"/api/v1/recipes/{Guid.NewGuid()}/favorite", content: null);

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	private async Task CleanupAsync()
	{
		var userId = await fixture.GetTestUserIdAsync();
		var owner = OwnerIdentifier.From(userId);
		await AspireFixture.CleanupAsync(
			fixture.CreateRecipesDbContextAsync,
			ctx => ctx.Recipes.Where(r => r.Owner == owner));
		await AspireFixture.CleanupAsync(
			fixture.CreateRecipesDbContextAsync,
			ctx => ctx.RecipeFavorites.Where(f => f.Owner == owner));
	}
}
