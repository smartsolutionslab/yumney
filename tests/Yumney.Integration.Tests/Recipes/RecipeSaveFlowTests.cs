using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Recipes;

[Collection(AspireCollection.Name)]
public class RecipeSaveFlowTests(AspireFixture fixture)
{
	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

	[Fact]
	public async Task Save_ValidRecipe_ReturnsCreated()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("recipes-api");

		var request = BuildRecipeRequest("Save Test Pasta");

		var response = await client.PostAsJsonAsync("/api/v1/recipes", request);

		response.StatusCode.Should().Be(HttpStatusCode.Created);
		response.Headers.Location.Should().NotBeNull();
	}

	[Fact]
	public async Task Save_ValidRecipe_ReturnsIdentifierAndTitle()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("recipes-api");

		var request = BuildRecipeRequest("Identifier Test Recipe");

		var response = await client.PostAsJsonAsync("/api/v1/recipes", request);
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

		body.GetProperty("identifier").GetGuid().Should().NotBeEmpty();
		body.GetProperty("title").GetString().Should().Be("Identifier Test Recipe");
	}

	[Fact]
	public async Task SaveAndRetrieve_PersistsAllFields()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("recipes-api");

		var request = new
		{
			title = "Full Recipe Test",
			description = "A complete test recipe",
			ingredients = new object[]
			{
				new { name = "Spaghetti", amount = 500m, unit = "g" },
				new { name = "Eggs", amount = 4m },
			},
			steps = new object[]
			{
				new { number = 1, description = "Boil pasta" },
				new { number = 2, description = "Mix eggs" },
			},
			servings = 4,
			prepTimeMinutes = 10,
			cookTimeMinutes = 20,
			difficulty = "medium",
			tags = new[] { "italian", "quick" },
		};

		var createResponse = await client.PostAsJsonAsync("/api/v1/recipes", request);
		createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
		var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		var id = created.GetProperty("identifier").GetGuid();

		var getResponse = await client.GetAsync($"/api/v1/recipes/{id}");
		getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
		var detail = await getResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

		detail.GetProperty("title").GetString().Should().Be("Full Recipe Test");
		detail.GetProperty("description").GetString().Should().Be("A complete test recipe");
		detail.GetProperty("servings").GetInt32().Should().Be(4);
		detail.GetProperty("prepTimeMinutes").GetInt32().Should().Be(10);
		detail.GetProperty("cookTimeMinutes").GetInt32().Should().Be(20);
		detail.GetProperty("difficulty").GetString().Should().Be("medium");
		detail.GetProperty("ingredients").GetArrayLength().Should().Be(2);
		detail.GetProperty("steps").GetArrayLength().Should().Be(2);
		detail.GetProperty("tags").GetArrayLength().Should().Be(2);
	}

	[Fact]
	public async Task SaveAndUpdate_PersistsChanges()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("recipes-api");

		var createRequest = BuildRecipeRequest("Before Update");
		var createResponse = await client.PostAsJsonAsync("/api/v1/recipes", createRequest);
		var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		var id = created.GetProperty("identifier").GetGuid();

		var updateRequest = new
		{
			title = "After Update",
			ingredients = new object[] { new { name = "Updated Ingredient", amount = 1m, unit = "pc" } },
			steps = new object[] { new { number = 1, description = "Updated step" } },
			servings = 2,
		};

		var updateResponse = await client.PutAsJsonAsync($"/api/v1/recipes/{id}", updateRequest);
		updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

		var getResponse = await client.GetAsync($"/api/v1/recipes/{id}");
		var detail = await getResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

		detail.GetProperty("title").GetString().Should().Be("After Update");
		detail.GetProperty("servings").GetInt32().Should().Be(2);
	}

	[Fact]
	public async Task SaveAndDelete_RemovesRecipe()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("recipes-api");

		var createResponse = await client.PostAsJsonAsync("/api/v1/recipes", BuildRecipeRequest("Delete Me"));
		var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		var id = created.GetProperty("identifier").GetGuid();

		var deleteResponse = await client.DeleteAsync($"/api/v1/recipes/{id}");
		deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

		var getResponse = await client.GetAsync($"/api/v1/recipes/{id}");
		getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task Save_EmptyTitle_ReturnsValidationError()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("recipes-api");

		var request = new
		{
			title = string.Empty,
			ingredients = new object[] { new { name = "Milk" } },
			steps = new object[] { new { number = 1, description = "Pour" } },
		};

		var response = await client.PostAsJsonAsync("/api/v1/recipes", request);

		response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
	}

	[Fact]
	public async Task Save_WithoutAuth_Returns401()
	{
		var client = fixture.RecipesApi;

		var response = await client.PostAsJsonAsync("/api/v1/recipes", BuildRecipeRequest("Unauthorized"));

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	private static object BuildRecipeRequest(string title) => new
	{
		title,
		ingredients = new object[] { new { name = "Test Ingredient", amount = 1m, unit = "pc" } },
		steps = new object[] { new { number = 1, description = "Test step" } },
	};
}
