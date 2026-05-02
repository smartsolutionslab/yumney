using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Recipes.Contract;

[Collection(AspireCollection.Name)]
public class GetRecipeSuggestionsContractTests(AspireFixture fixture)
{
	private const string Endpoint = "/api/v1/recipes/suggestions";

	[Fact]
	public async Task GetSuggestions_WithoutAuth_Returns401()
	{
		var client = fixture.RecipesApi;

		var response = await client.GetAsync(Endpoint);

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}
}
