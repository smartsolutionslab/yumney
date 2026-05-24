using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Recipes.Contract;

[Collection(AspireCollection.Name)]
public class ImportRecipeContractTests(AspireFixture fixture)
{
	private const string Endpoint = "/api/v1/recipes/import";

	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

	[Fact]
	public async Task Import_ValidUrl_Returns200WithSavedRecipe()
	{
		// Shape changed in #820: the endpoint now extracts AND persists, so the
		// response carries the saved recipe identifier (SavedRecipeDto) instead
		// of the full extracted shape with ingredients/steps. The stub-recipe
		// E2E mode still drives the title. Unique URL per run avoids the
		// AlreadyImported guard if this test re-runs against the same fixture.
		using var client = await fixture.CreateAuthenticatedClientAsync("recipes-api");

		var response = await client.PostAsJsonAsync(Endpoint, new { url = $"https://example.com/recipe-{Guid.NewGuid():N}" });

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		body.GetProperty("title").GetString().Should().Be("Stub Recipe");
		body.GetProperty("identifier").GetGuid().Should().NotBe(Guid.Empty);
		body.GetProperty("createdAt").GetDateTime().Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
	}

	[Fact]
	public async Task Import_WithoutAuth_Returns401()
	{
		var client = fixture.RecipesApi;

		var response = await client.PostAsJsonAsync(Endpoint, new { url = "https://example.com/recipe" });

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task Import_EmptyUrl_Returns422()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("recipes-api");

		var response = await client.PostAsJsonAsync(Endpoint, new { url = string.Empty });

		response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
	}

	[Fact]
	public async Task Import_MalformedUrl_Returns422()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("recipes-api");

		var response = await client.PostAsJsonAsync(Endpoint, new { url = "not-a-url" });

		response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
	}
}
