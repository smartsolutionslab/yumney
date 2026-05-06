using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Recipes.Contract;

[Collection(AspireCollection.Name)]
public class RecognizeIngredientsContractTests(AspireFixture fixture)
{
	private const string Endpoint = "/api/v1/recipes/recognize-ingredients";

	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

	[Fact]
	public async Task RecognizeIngredients_ValidPhoto_Returns200WithIngredientsArray()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("recipes-api");
		using var content = BuildJpegMultipart();

		var response = await client.PostAsync(Endpoint, content);

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		body.GetProperty("ingredients").GetArrayLength().Should().BeGreaterThan(0);
	}

	[Fact]
	public async Task RecognizeIngredients_WithoutAuth_Returns401()
	{
		var client = fixture.RecipesApi;
		using var content = BuildJpegMultipart();

		var response = await client.PostAsync(Endpoint, content);

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task RecognizeIngredients_UnsupportedContentType_Returns422()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("recipes-api");
		using var content = new MultipartFormDataContent();
		var bytes = new byte[] { 0x00, 0x01, 0x02 };
		var photo = new ByteArrayContent(bytes);
		photo.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
		content.Add(photo, "photo", "note.txt");

		var response = await client.PostAsync(Endpoint, content);

		response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
	}

	private static MultipartFormDataContent BuildJpegMultipart()
	{
		var content = new MultipartFormDataContent();
		var bytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
		var photo = new ByteArrayContent(bytes);
		photo.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
		content.Add(photo, "photo", "photo.jpg");
		return content;
	}
}
