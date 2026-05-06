using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Recipes.Contract;

[Collection(AspireCollection.Name)]
public class ImportRecipeFromTextContractTests(AspireFixture fixture)
{
	private const string Endpoint = "/api/v1/recipes/import-from-text";

	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

	[Fact]
	public async Task ImportFromText_ValidText_Returns200WithExtractedRecipe()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("recipes-api");

		var response = await client.PostAsJsonAsync(Endpoint, new { text = "Mix flour with water. Bake for 20 minutes." });

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		body.GetProperty("title").GetString().Should().Be("Stub Recipe");
	}

	[Fact]
	public async Task ImportFromText_WithoutAuth_Returns401()
	{
		var client = fixture.RecipesApi;

		var response = await client.PostAsJsonAsync(Endpoint, new { text = "Some recipe text" });

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task ImportFromText_EmptyText_Returns422()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("recipes-api");

		var response = await client.PostAsJsonAsync(Endpoint, new { text = string.Empty });

		response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
	}
}
