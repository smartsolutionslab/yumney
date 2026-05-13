using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Recipes.Contract;

[Collection(AspireCollection.Name)]
public class GetRecipeSuggestionsContractTests(AspireFixture fixture)
{
	private const string Endpoint = "/api/v1/recipes/suggestions";

	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

	[Fact]
	public async Task GetSuggestions_AuthenticatedRequest_Returns200WithSuggestionsArray()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("recipes-api");

		var response = await client.GetAsync($"{Endpoint}?count=2");

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		body.ValueKind.Should().Be(JsonValueKind.Array);
		body.GetArrayLength().Should().BeGreaterThan(0);
	}

	[Fact]
	public async Task GetSuggestions_WithoutAuth_Returns401()
	{
		var client = fixture.RecipesApi;

		var response = await client.GetAsync(Endpoint);

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}
}
