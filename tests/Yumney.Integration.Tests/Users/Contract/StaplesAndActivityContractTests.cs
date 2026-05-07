using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Users.Contract;

/// <summary>
/// Contract tests for the remaining authenticated Users endpoints:
/// GET /users/staples, GET /users/me/activity, GET /users/me/suggestions.
/// All require auth; all return JSON payloads tied to the current user.
/// </summary>
[Collection(AspireCollection.Name)]
public class StaplesAndActivityContractTests(AspireFixture fixture)
{
	private const string Staples = "/api/v1/users/staples";
	private const string Activity = "/api/v1/users/me/activity";
	private const string Suggestions = "/api/v1/users/me/suggestions";

	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

	[Fact]
	public async Task GetStaples_Authenticated_Returns200WithJsonArray()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("users-api");

		var response = await client.GetAsync(Staples);

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		body.ValueKind.Should().Be(JsonValueKind.Array);
	}

	[Fact]
	public async Task GetStaples_WithoutAuth_Returns401()
	{
		var client = fixture.UsersApi;

		var response = await client.GetAsync(Staples);

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task GetRecentActivity_Authenticated_Returns200WithCursorPage()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("users-api");

		var response = await client.GetAsync(Activity);

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		body.ValueKind.Should().Be(JsonValueKind.Object);
		body.GetProperty("items").ValueKind.Should().Be(JsonValueKind.Array);
		body.TryGetProperty("nextCursor", out _).Should().BeTrue();
	}

	[Fact]
	public async Task GetRecentActivity_WithLimitParameter_Returns200()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("users-api");

		var response = await client.GetAsync($"{Activity}?limit=3");

		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task GetRecentActivity_InvalidLimitOutOfRange_Returns400GuardFailure()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("users-api");

		var response = await client.GetAsync($"{Activity}?limit=0");

		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task GetRecentActivity_WithoutAuth_Returns401()
	{
		var client = fixture.UsersApi;

		var response = await client.GetAsync(Activity);

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task GetSuggestions_Authenticated_Returns200WithObject()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("users-api");

		var response = await client.GetAsync(Suggestions);

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		body.ValueKind.Should().Be(JsonValueKind.Object);
	}

	[Fact]
	public async Task GetSuggestions_WithoutAuth_Returns401()
	{
		var client = fixture.UsersApi;

		var response = await client.GetAsync(Suggestions);

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}
}
