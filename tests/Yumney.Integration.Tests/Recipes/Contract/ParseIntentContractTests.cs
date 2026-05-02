using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Recipes.Contract;

[Collection(AspireCollection.Name)]
public class ParseIntentContractTests(AspireFixture fixture)
{
	private const string Endpoint = "/api/v1/recipes/parse-intent";

	[Fact]
	public async Task ParseIntent_WithoutAuth_Returns401()
	{
		var client = fixture.RecipesApi;

		var response = await client.PostAsJsonAsync(Endpoint, new { message = "find me a pasta recipe", context = (string?)null });

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task ParseIntent_EmptyMessage_Returns422()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("recipes-api");

		var response = await client.PostAsJsonAsync(Endpoint, new { message = string.Empty, context = (string?)null });

		response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
	}
}
