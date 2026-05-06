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
public class ImportRecipeFromPhotosContractTests(AspireFixture fixture)
{
	private const string Endpoint = "/api/v1/recipes/import-from-photos";

	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

	[Fact]
	public async Task ImportFromPhotos_ValidPhoto_Returns200WithExtractedRecipe()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("recipes-api");
		using var content = BuildMultipartWithPhotos(1);

		var response = await client.PostAsync(Endpoint, content);

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		body.GetProperty("title").GetString().Should().Be("Stub Recipe");
	}

	[Fact]
	public async Task ImportFromPhotos_WithoutAuth_Returns401()
	{
		var client = fixture.RecipesApi;
		using var content = BuildMultipartWithPhotos(1);

		var response = await client.PostAsync(Endpoint, content);

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task ImportFromPhotos_TooManyPhotos_Returns400()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("recipes-api");
		using var content = BuildMultipartWithPhotos(11);

		var response = await client.PostAsync(Endpoint, content);

		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task ImportFromPhotos_UnsupportedContentType_Returns422()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("recipes-api");
		using var content = new MultipartFormDataContent();
		var bytes = new byte[] { 0x00, 0x01, 0x02 };
		var photo = new ByteArrayContent(bytes);
		photo.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
		content.Add(photo, "photos", "note.txt");

		var response = await client.PostAsync(Endpoint, content);

		response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
	}

	private static MultipartFormDataContent BuildMultipartWithPhotos(int count)
	{
		var content = new MultipartFormDataContent();
		var bytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }; // JPEG SOI marker
		for (var index = 0; index < count; index++)
		{
			var photo = new ByteArrayContent(bytes);
			photo.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
			content.Add(photo, "photos", $"photo-{index}.jpg");
		}

		return content;
	}
}
