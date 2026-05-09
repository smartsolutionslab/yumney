using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Users.Contract;

/// <summary>
/// Contract tests for /api/v1/users/me/profile (GET and PUT).
/// Both require authentication. GET returns 404 if no profile row exists for
/// the Keycloak user.
/// </summary>
[Collection(AspireCollection.Name)]
public class ProfileEndpointsContractTests(AspireFixture fixture) : IAsyncLifetime
{
	private const string Endpoint = "/api/v1/users/me/profile";

	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

	private KeycloakUserId? keycloakId;

	public async Task InitializeAsync()
	{
		var userId = await fixture.GetTestUserIdAsync();
		keycloakId = KeycloakUserId.From(userId);
		await CleanupProfileAsync();
	}

	public Task DisposeAsync() => CleanupProfileAsync();

	[Fact]
	public async Task GetProfile_NoProfileRow_JitProvisionsFromClaims()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("users-api");

		var response = await client.GetAsync(Endpoint);

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		body.GetProperty("displayName").GetString().Should().NotBeNullOrWhiteSpace();
	}

	[Fact]
	public async Task GetProfile_ExistingProfile_ReturnsDto()
	{
		await fixture.SeedUserProfilesAsync(AppUserProfile.Create(keycloakId!, DisplayName.From("Test User")));
		using var client = await fixture.CreateAuthenticatedClientAsync("users-api");

		var response = await client.GetAsync(Endpoint);

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		body.GetProperty("displayName").GetString().Should().Be("Test User");
		body.GetProperty("defaultServings").GetInt32().Should().BeGreaterThan(0);
	}

	[Fact]
	public async Task GetProfile_WithoutAuth_Returns401()
	{
		var client = fixture.UsersApi;

		var response = await client.GetAsync(Endpoint);

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task UpdateProfile_ValidInput_Returns200AndPersistsChanges()
	{
		await fixture.SeedUserProfilesAsync(AppUserProfile.Create(keycloakId!, DisplayName.From("Test User")));
		using var client = await fixture.CreateAuthenticatedClientAsync("users-api");

		var response = await client.PutAsJsonAsync(Endpoint, new
		{
			defaultServings = 6,
			dietaryType = "vegetarian",
			restrictions = Array.Empty<string>(),
			minVeggieMeals = (int?)null,
			maxRedMeatMeals = (int?)null,
			cookingEffort = (string?)null,
		});

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		body.GetProperty("defaultServings").GetInt32().Should().Be(6);
	}

	[Fact]
	public async Task UpdateProfile_DefaultServingsOutOfRange_Returns422ValidationProblem()
	{
		await fixture.SeedUserProfilesAsync(AppUserProfile.Create(keycloakId!, DisplayName.From("Test User")));
		using var client = await fixture.CreateAuthenticatedClientAsync("users-api");

		var response = await client.PutAsJsonAsync(Endpoint, new
		{
			defaultServings = 0,
			dietaryType = (string?)null,
			restrictions = Array.Empty<string>(),
			minVeggieMeals = (int?)null,
			maxRedMeatMeals = (int?)null,
			cookingEffort = (string?)null,
		});

		response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
	}

	[Fact]
	public async Task UpdateProfile_WithoutAuth_Returns401()
	{
		var client = fixture.UsersApi;

		var response = await client.PutAsJsonAsync(Endpoint, new
		{
			defaultServings = 4,
			restrictions = Array.Empty<string>(),
		});

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	private Task CleanupProfileAsync()
	{
		var id = keycloakId;
		if (id is null) return Task.CompletedTask;
		return AspireFixture.CleanupAsync(
			fixture.CreateUsersDbContextAsync,
			ctx => ctx.AppUserProfiles.Where(profile => profile.KeycloakUserId == id));
	}
}
