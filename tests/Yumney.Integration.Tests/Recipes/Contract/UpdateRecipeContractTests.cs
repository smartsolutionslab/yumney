using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Recipes.Contract;

/// <summary>
/// Contract tests for PUT /api/v1/recipes/{id}. Cross-user 404 lives in
/// CrossModule/OwnershipIsolationTests; this file covers the owner happy
/// path (round-trip via GET), the missing-id 404, the auth boundary, and
/// FluentValidation-driven 422 paths.
/// </summary>
[Collection(AspireCollection.Name)]
public class UpdateRecipeContractTests(AspireFixture fixture) : IAsyncLifetime
{
	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

	public Task InitializeAsync() => CleanupAsync();

	public Task DisposeAsync() => CleanupAsync();

	[Fact]
	public async Task UpdateRecipe_OwnerEditsTitleAndServings_RoundTripReturnsNewValues()
	{
		var userId = await fixture.GetTestUserIdAsync();
		var recipe = RecipeFactory.Lasagne(userId);
		await fixture.SeedRecipesAsync(recipe);
		using var client = await fixture.CreateAuthenticatedClientAsync("recipes-api");

		var update = new
		{
			title = "Updated Lasagne",
			ingredients = new object[] { new { name = "Tomato", amount = 800m, unit = "g" } },
			steps = new object[] { new { number = 1, description = "Stir." } },
			servings = 8,
			description = "Now with more tomato.",
		};

		var putResponse = await client.PutAsJsonAsync($"/api/v1/recipes/{recipe.Id.Value}", update);
		putResponse.StatusCode.Should().Be(HttpStatusCode.OK);

		var getResponse = await client.GetAsync($"/api/v1/recipes/{recipe.Id.Value}");
		getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
		var body = await getResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		body.GetProperty("title").GetString().Should().Be("Updated Lasagne");
		body.GetProperty("servings").GetInt32().Should().Be(8);
		body.GetProperty("description").GetString().Should().Be("Now with more tomato.");
	}

	[Fact]
	public async Task UpdateRecipe_NonExistentId_Returns404()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("recipes-api");
		var update = new
		{
			title = "Anything",
			ingredients = new object[] { new { name = "Salt", amount = 1m, unit = "g" } },
			steps = new object[] { new { number = 1, description = "Mix." } },
		};

		var response = await client.PutAsJsonAsync($"/api/v1/recipes/{Guid.NewGuid()}", update);

		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task UpdateRecipe_EmptyTitle_Returns422()
	{
		var userId = await fixture.GetTestUserIdAsync();
		var recipe = RecipeFactory.Lasagne(userId);
		await fixture.SeedRecipesAsync(recipe);
		using var client = await fixture.CreateAuthenticatedClientAsync("recipes-api");
		var invalid = new
		{
			title = string.Empty,
			ingredients = new object[] { new { name = "Salt", amount = 1m, unit = "g" } },
			steps = new object[] { new { number = 1, description = "Mix." } },
		};

		var response = await client.PutAsJsonAsync($"/api/v1/recipes/{recipe.Id.Value}", invalid);

		response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
	}

	[Fact]
	public async Task UpdateRecipe_EmptyIngredientsList_Returns422()
	{
		var userId = await fixture.GetTestUserIdAsync();
		var recipe = RecipeFactory.Lasagne(userId);
		await fixture.SeedRecipesAsync(recipe);
		using var client = await fixture.CreateAuthenticatedClientAsync("recipes-api");
		var invalid = new
		{
			title = "Still Valid Title",
			ingredients = Array.Empty<object>(),
			steps = new object[] { new { number = 1, description = "Mix." } },
		};

		var response = await client.PutAsJsonAsync($"/api/v1/recipes/{recipe.Id.Value}", invalid);

		response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
	}

	[Fact]
	public async Task UpdateRecipe_Unauthenticated_Returns401()
	{
		var client = fixture.RecipesApi;
		var update = new
		{
			title = "Anything",
			ingredients = new object[] { new { name = "Salt", amount = 1m, unit = "g" } },
			steps = new object[] { new { number = 1, description = "Mix." } },
		};

		var response = await client.PutAsJsonAsync($"/api/v1/recipes/{Guid.NewGuid()}", update);

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
