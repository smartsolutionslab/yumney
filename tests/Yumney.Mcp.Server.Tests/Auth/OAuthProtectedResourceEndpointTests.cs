using System.Text.Json;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Mcp.Server.Auth;
using Xunit;

namespace SmartSolutionsLab.Yumney.Mcp.Server.Tests.Auth;

public class OAuthProtectedResourceEndpointTests
{
	[Fact]
	public void BuildDocument_SetsResourceAndAuthorizationServers()
	{
		var doc = OAuthProtectedResourceEndpoint.BuildDocument(
			resourceUrl: "https://yumney-gateway.example.com/mcp",
			authorizationServerUrl: "https://yumney-keycloak.example.com/realms/yumney");

		doc.Resource.Should().Be("https://yumney-gateway.example.com/mcp");
		doc.AuthorizationServers.Should().ContainSingle().Which.Should().Be("https://yumney-keycloak.example.com/realms/yumney");
	}

	[Fact]
	public void BuildDocument_AlwaysAdvertisesHeaderBearerMethod()
	{
		var doc = OAuthProtectedResourceEndpoint.BuildDocument("https://r", "https://as");

		doc.BearerMethodsSupported.Should().ContainSingle().Which.Should().Be("header");
	}

	[Fact]
	public void BuildDocument_IncludesYumneyApiScope()
	{
		var doc = OAuthProtectedResourceEndpoint.BuildDocument("https://r", "https://as");

		doc.ScopesSupported.Should().Contain("yumney-api");
		doc.ScopesSupported.Should().Contain("openid");
	}

	[Fact]
	public void BuildDocument_SerializesWithSnakeCaseJsonPropertyNames()
	{
		var doc = OAuthProtectedResourceEndpoint.BuildDocument(
			"https://yumney-gateway.example.com/mcp",
			"https://yumney-keycloak.example.com/realms/yumney");

		var json = JsonSerializer.Serialize(doc);

		json.Should().Contain("\"resource\"");
		json.Should().Contain("\"authorization_servers\"");
		json.Should().Contain("\"bearer_methods_supported\"");
		json.Should().Contain("\"scopes_supported\"");
		json.Should().NotContain("\"Resource\"");
	}

	[Theory]
	[InlineData("https", "yumney-gateway.example.com", "https://yumney-gateway.example.com/mcp")]
	[InlineData("http", "localhost:5000", "http://localhost:5000/mcp")]
	[InlineData("https", "yumney.app", "https://yumney.app/mcp")]
	public void InferResourceUrl_ComposesSchemeHostAndMcpPath(string scheme, string host, string expected)
	{
		OAuthProtectedResourceEndpoint.InferResourceUrl(scheme, host).Should().Be(expected);
	}
}
