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
	public async Task Import_ValidUrl_Returns200WithExtractedRecipe()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("recipes-api");

		var response = await client.PostAsJsonAsync(Endpoint, new { url = "https://example.com/recipe" });

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		body.GetProperty("title").GetString().Should().Be("Stub Recipe");
		body.GetProperty("ingredients").GetArrayLength().Should().BeGreaterThan(0);
		body.GetProperty("steps").GetArrayLength().Should().BeGreaterThan(0);
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
