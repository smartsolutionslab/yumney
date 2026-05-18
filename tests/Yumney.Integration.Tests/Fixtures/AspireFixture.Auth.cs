using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Aspire.Hosting.Testing;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;

#pragma warning disable SA1601
public sealed partial class AspireFixture
#pragma warning restore SA1601
{
	public Task<HttpClient> CreateAuthenticatedClientAsync(string resourceName) =>
		CreateAuthenticatedClientAsync(resourceName, "testuser", "Test1234");

	public async Task<HttpClient> CreateAuthenticatedClientAsync(string resourceName, string username, string password)
	{
		var accessToken = await GetAccessTokenAsync(username, password);
		var client = App.CreateHttpClient(resourceName);
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

		return client;
	}

	public Task<string> GetTestUserIdAsync() => GetUserIdAsync("testuser", "Test1234");

	public async Task<string> GetUserIdAsync(string username, string password)
	{
		var accessToken = await GetAccessTokenAsync(username, password);
		var payload = accessToken.Split('.')[1];
		var padded = payload.PadRight(payload.Length + ((4 - (payload.Length % 4)) % 4), '=');
		var decoded = Convert.FromBase64String(padded.Replace('-', '+').Replace('_', '/'));
		var claims = JsonSerializer.Deserialize<JsonElement>(decoded);

		return claims.GetProperty("sub").GetString()!;
	}

	public async Task<string> GetAccessTokenAsync(string username, string password)
	{
		var keycloakClient = App.CreateHttpClient("keycloak");
		Dictionary<string, string> valueCollection = new()
		{
			["grant_type"] = "password",
			["client_id"] = "yumney-web",
			["username"] = username,
			["password"] = password,
		};
		var tokenResponse = await keycloakClient.PostAsync("/realms/yumney/protocol/openid-connect/token", new FormUrlEncodedContent(valueCollection));

		if (!tokenResponse.IsSuccessStatusCode)
		{
			var body = await tokenResponse.Content.ReadAsStringAsync();
			throw new InvalidOperationException($"Keycloak password grant failed for '{username}': {tokenResponse.StatusCode} {body}");
		}

		var tokenJson = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();

		return tokenJson.GetProperty("access_token").GetString()!;
	}

	/// <summary>
	/// Provision a brand-new Keycloak user via the admin API with emailVerified=true
	/// and no requiredActions, so password-grant login works immediately.
	/// /auth/register can't be used here because the registration handler creates
	/// users with a VERIFY_EMAIL requiredAction that blocks login regardless of the
	/// realm's verifyEmail setting.
	/// </summary>
	/// <param name="emailPrefix">Prefix for the generated email address; final form is {prefix}-{guid}@yumney.dev.</param>
	/// <returns>Tuple of (KeycloakUserId from the new user's <c>sub</c> claim, generated Email, generated Password). Caller is responsible for cleanup if the test doesn't delete the account.</returns>
	public async Task<(string KeycloakUserId, string Email, string Password)> CreateKeycloakUserAsync(string emailPrefix = "test")
	{
		const string password = "Valid1Pass";
		var email = $"{emailPrefix}-{Guid.NewGuid():N}@yumney.dev";

		var keycloak = App.CreateHttpClient("keycloak");
		var adminToken = await GetMasterRealmAdminTokenAsync(keycloak);

		using var request = new HttpRequestMessage(HttpMethod.Post, "/admin/realms/yumney/users")
		{
			Content = JsonContent.Create(new
			{
				username = email,
				email,
				enabled = true,
				emailVerified = true,
				firstName = "Test",
				lastName = "User",
				credentials = new[] { new { type = "password", value = password, temporary = false } },
				realmRoles = DefaultUserRealmRoles,
				requiredActions = Array.Empty<string>(),
			}),
		};
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

		var response = await keycloak.SendAsync(request);
		if (!response.IsSuccessStatusCode)
		{
			var body = await response.Content.ReadAsStringAsync();
			throw new InvalidOperationException($"Keycloak admin user creation failed: {response.StatusCode} {body}");
		}

		// Location header is /admin/realms/yumney/users/{guid}
		var location = response.Headers.Location?.ToString()
			?? throw new InvalidOperationException("Keycloak did not return a Location header for the created user");
		var keycloakUserId = location.Split('/').Last();

		// The realm applies default required actions (Update Password, …) at
		// user-creation time and ignores requiredActions in the create payload.
		// PUT after-the-fact to clear them, otherwise password-grant fails with
		// "Account is not fully set up".
		using var clearActions = new HttpRequestMessage(HttpMethod.Put, $"/admin/realms/yumney/users/{keycloakUserId}")
		{
			Content = JsonContent.Create(new { requiredActions = Array.Empty<string>() }),
		};
		clearActions.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
		var clearResponse = await keycloak.SendAsync(clearActions);
		if (!clearResponse.IsSuccessStatusCode)
		{
			var body = await clearResponse.Content.ReadAsStringAsync();
			throw new InvalidOperationException($"Failed to clear required actions on Keycloak user {keycloakUserId}: {clearResponse.StatusCode} {body}");
		}

		return (keycloakUserId, email, password);
	}

	private static async Task<string> GetMasterRealmAdminTokenAsync(HttpClient keycloak)
	{
		var form = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			["grant_type"] = "password",
			["client_id"] = "admin-cli",
			["username"] = "admin",
			["password"] = "testkeycloak",
		});
		var response = await keycloak.PostAsync("/realms/master/protocol/openid-connect/token", form);
		response.EnsureSuccessStatusCode();
		var tokenJson = await response.Content.ReadFromJsonAsync<JsonElement>();
		return tokenJson.GetProperty("access_token").GetString()!;
	}
}
