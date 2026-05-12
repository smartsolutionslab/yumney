using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Users.Contract;

/// <summary>
/// Contract tests for /api/v1/users/me (DELETE — GDPR Art. 17 / US-101).
/// Requires authentication. Always uses a freshly-registered user so the
/// shared <c>testuser</c> account remains usable for the rest of the suite.
/// </summary>
[Collection(AspireCollection.Name)]
public class DangerZoneEndpointsContractTests(AspireFixture fixture)
{
	private const string Endpoint = "/api/v1/users/me";

	[Fact]
	public async Task DeleteAccount_WithoutToken_Returns401()
	{
		var response = await fixture.UsersApi.DeleteAsync(Endpoint);

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task DeleteAccount_WithFreshlyRegisteredUser_Returns204()
	{
		var (email, password) = ($"contract-delete-{Guid.NewGuid():N}@yumney.dev", "Valid1Pass");
		await RegisterUserAsync(email, password);
		var token = await fixture.GetAccessTokenAsync(email, password);

		using var client = AuthenticatedClient(token);

		var response = await client.DeleteAsync(Endpoint);

		response.StatusCode.Should().Be(HttpStatusCode.NoContent);
	}

	private async Task RegisterUserAsync(string email, string password)
	{
		var response = await fixture.UsersApi.PostAsJsonAsync("/api/v1/auth/register", new
		{
			email,
			password,
			displayName = $"Contract {email}",
		});
		response.EnsureSuccessStatusCode();
	}

	private HttpClient AuthenticatedClient(string token)
	{
		var client = new HttpClient { BaseAddress = fixture.UsersApi.BaseAddress };
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
		return client;
	}
}
