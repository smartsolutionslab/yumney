using System;
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
public class ChatContractTests(AspireFixture fixture)
{
	private const string Endpoint = "/api/v1/recipes/chat";

	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

	[Fact]
	public async Task Chat_ValidMessage_Returns200WithReply()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("recipes-api");

		var response = await client.PostAsJsonAsync(Endpoint, new { message = "What can I cook?", history = Array.Empty<object>() });

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		body.GetProperty("reply").GetString().Should().NotBeNullOrEmpty();
	}

	[Fact]
	public async Task Chat_WithoutAuth_Returns401()
	{
		var client = fixture.RecipesApi;

		var response = await client.PostAsJsonAsync(Endpoint, new { message = "hello", history = Array.Empty<object>() });

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task Chat_EmptyMessage_Returns422()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("recipes-api");

		var response = await client.PostAsJsonAsync(Endpoint, new { message = string.Empty, history = Array.Empty<object>() });

		response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
	}
}
