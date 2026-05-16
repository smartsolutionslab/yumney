using FluentAssertions;
using Microsoft.AspNetCore.Http;
using SmartSolutionsLab.Yumney.Mcp.Server.Auth;
using Xunit;

namespace SmartSolutionsLab.Yumney.Mcp.Server.Tests.Auth;

public class WwwAuthenticateChallengeTests
{
	[Fact]
	public void BuildHeader_WithError_IncludesAllThreeParameters()
	{
		var header = WwwAuthenticateChallenge.BuildHeader(
			realm: "yumney",
			discoveryUrl: "https://yumney-mcp.example.com/.well-known/oauth-protected-resource",
			error: "invalid_token");

		header.Should().Be(
			"Bearer realm=\"yumney\", " +
			"resource_metadata=\"https://yumney-mcp.example.com/.well-known/oauth-protected-resource\", " +
			"error=\"invalid_token\"");
	}

	[Fact]
	public void BuildHeader_WithNullError_OmitsErrorParameter()
	{
		var header = WwwAuthenticateChallenge.BuildHeader(
			realm: "yumney",
			discoveryUrl: "https://example.com/.well-known/oauth-protected-resource",
			error: null);

		header.Should().NotContain("error=");
		header.Should().Be("Bearer realm=\"yumney\", resource_metadata=\"https://example.com/.well-known/oauth-protected-resource\"");
	}

	[Fact]
	public void BuildHeader_WithEmptyError_OmitsErrorParameter()
	{
		var header = WwwAuthenticateChallenge.BuildHeader("yumney", "https://example.com/disco", string.Empty);

		header.Should().NotContain("error=");
	}

	[Fact]
	public void ResolveDiscoveryUrl_WithConfiguredResourceUrl_ReusesOriginAndAppendsDiscoveryPath()
	{
		var url = WwwAuthenticateChallenge.ResolveDiscoveryUrl(
			request: new DefaultHttpContext().Request,
			configuredResourceUrl: "https://yumney-gateway.example.com/mcp");

		url.Should().Be("https://yumney-gateway.example.com/.well-known/oauth-protected-resource");
	}

	[Fact]
	public void ResolveDiscoveryUrl_WithConfiguredUrlIncludingPort_PreservesPort()
	{
		var url = WwwAuthenticateChallenge.ResolveDiscoveryUrl(
			request: new DefaultHttpContext().Request,
			configuredResourceUrl: "https://yumney.local:8443/mcp");

		url.Should().Be("https://yumney.local:8443/.well-known/oauth-protected-resource");
	}

	[Fact]
	public void ResolveDiscoveryUrl_WithoutOverride_BuildsFromRequest()
	{
		var context = new DefaultHttpContext();
		context.Request.Scheme = "https";
		context.Request.Host = new HostString("yumney.app");

		var url = WwwAuthenticateChallenge.ResolveDiscoveryUrl(context.Request, configuredResourceUrl: null);

		url.Should().Be("https://yumney.app/.well-known/oauth-protected-resource");
	}

	[Fact]
	public void ResolveDiscoveryUrl_WithWhitespaceOverride_FallsBackToRequest()
	{
		var context = new DefaultHttpContext();
		context.Request.Scheme = "http";
		context.Request.Host = new HostString("localhost:5000");

		var url = WwwAuthenticateChallenge.ResolveDiscoveryUrl(context.Request, configuredResourceUrl: "   ");

		url.Should().Be("http://localhost:5000/.well-known/oauth-protected-resource");
	}
}
