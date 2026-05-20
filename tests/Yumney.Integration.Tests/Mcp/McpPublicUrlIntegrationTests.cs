using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Mcp;

/// <summary>
/// Coverage for the fix in PR #804: the MCP server's <c>WWW-Authenticate</c>
/// challenge MUST advertise a discovery URL on the public gateway origin,
/// and that discovery URL MUST be routable through the gateway. Without
/// either, external MCP clients (Claude, ChatGPT) can't complete the OAuth
/// dance and connector setup fails before any tool call.
/// </summary>
[Collection(AspireCollection.Name)]
public class McpPublicUrlIntegrationTests(AspireFixture fixture)
{
	[Fact]
	public async Task GetMcp_ThroughGateway_AdvertisesDiscoveryUrlOnGatewayOrigin()
	{
		var response = await fixture.Gateway.GetAsync("/mcp");

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
		response.Headers.WwwAuthenticate.Should().NotBeEmpty();

		var header = response.Headers.WwwAuthenticate.ToString();
		header.Should().Contain("resource_metadata=");

		// Extract the resource_metadata value and parse it.
		var resourceMetadataUrl = ExtractResourceMetadata(header);
		var resourceMetadataUri = new Uri(resourceMetadataUrl);

		// The URL must share the gateway's authority (host + port), NOT the
		// internal MCP container's. `fixture.Gateway.BaseAddress` is the URL
		// Aspire allocated for the gateway resource — that's the public origin.
		resourceMetadataUri.Authority.Should().Be(fixture.Gateway.BaseAddress!.Authority);
		resourceMetadataUri.AbsolutePath.Should().Be("/.well-known/oauth-protected-resource");
	}

	[Fact]
	public async Task GetDiscoveryDocument_ThroughGateway_ReturnsRfc9728Document()
	{
		var response = await fixture.Gateway.GetAsync("/.well-known/oauth-protected-resource");

		// Specifically NOT routed to the SPA catch-all (which would return text/html).
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

		var document = await response.Content.ReadFromJsonAsync<JsonElement>();

		// RFC 9728 §2 required fields.
		document.GetProperty("resource").GetString().Should().NotBeNullOrWhiteSpace();
		document.GetProperty("authorization_servers").GetArrayLength().Should().BeGreaterThan(0);

		// The resource URL in the document points at the public /mcp endpoint —
		// the same origin Claude initially requested.
		var resourceUrl = document.GetProperty("resource").GetString()!;
		new Uri(resourceUrl).Authority.Should().Be(fixture.Gateway.BaseAddress!.Authority);
		new Uri(resourceUrl).AbsolutePath.Should().Be("/mcp");
	}

	private static string ExtractResourceMetadata(string headerValue)
	{
		const string marker = "resource_metadata=\"";
		var start = headerValue.IndexOf(marker, StringComparison.Ordinal);
		start.Should().BeGreaterThan(-1, "the challenge header must include resource_metadata");
		start += marker.Length;
		var end = headerValue.IndexOf('"', start);
		return headerValue[start..end];
	}
}
