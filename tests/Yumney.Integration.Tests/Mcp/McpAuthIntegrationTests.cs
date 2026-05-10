using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Mcp;

/// <summary>
/// Phase 4c (#650) coverage: the /mcp endpoint requires a Keycloak-issued JWT.
/// Verifies the route is wired and JWT bearer auth is enforced — without this,
/// the REST proxy would silently accept any caller and forward an unforwardable
/// (missing) bearer to the modules.
/// </summary>
[Collection(AspireCollection.Name)]
public class McpAuthIntegrationTests(AspireFixture fixture)
{
	[Fact]
	public async Task PostToMcp_WithoutBearer_IsRejectedAsUnauthorized()
	{
		using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp");
		var response = await fixture.McpServer.SendAsync(request);

		// MCP HTTP transport accepts POST for JSON-RPC; without auth the
		// JWT bearer middleware short-circuits to 401.
		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task PostToMcp_WithInvalidBearer_IsRejectedAsUnauthorized()
	{
		using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp");
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-real-token");

		var response = await fixture.McpServer.SendAsync(request);

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task PostToMcp_WithValidKeycloakBearer_IsNotUnauthorized()
	{
		var token = await fixture.GetAccessTokenAsync("testuser", "Test1234");
		using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp");
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

		var response = await fixture.McpServer.SendAsync(request);

		// We don't assert OK here — the body is empty so MCP returns a JSON-RPC
		// parse-error 400 or 415 depending on transport state. The point of this
		// test is the AUTH gate: 401 means JWT validation failed against Keycloak,
		// and that's what we're proving doesn't happen with a valid token.
		response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task GetDiscoveredCapabilities_IsAnonymous_NoBearerNeeded()
	{
		var response = await fixture.McpServer.GetAsync("/discovered-capabilities");

		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}
}
