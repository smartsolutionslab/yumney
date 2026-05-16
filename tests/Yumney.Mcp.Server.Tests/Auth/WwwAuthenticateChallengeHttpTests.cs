using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartSolutionsLab.Yumney.Mcp.Server.Auth;
using Xunit;

namespace SmartSolutionsLab.Yumney.Mcp.Server.Tests.Auth;

public class WwwAuthenticateChallengeHttpTests
{
	[Fact]
	public async Task AnonymousRequest_ToProtectedRoute_Returns401WithDiscoveryMetadata()
	{
		using var server = BuildServer(publicResourceUrl: "https://yumney-mcp.example.com/mcp");
		using var client = server.CreateClient();

		var response = await client.GetAsync("/protected");

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
		var header = response.Headers.GetValues("WWW-Authenticate").Single();
		header.Should().StartWith("Bearer ");
		header.Should().Contain("realm=\"yumney\"");
		header.Should().Contain("resource_metadata=\"https://yumney-mcp.example.com/.well-known/oauth-protected-resource\"");
	}

	[Fact]
	public async Task AnonymousRequest_OmitsErrorParameter()
	{
		using var server = BuildServer(publicResourceUrl: "https://yumney-mcp.example.com/mcp");
		using var client = server.CreateClient();

		var response = await client.GetAsync("/protected");

		var header = response.Headers.GetValues("WWW-Authenticate").Single();
		header.Should().NotContain("error=");
	}

	[Fact]
	public async Task AnonymousRequest_WithoutPublicUrlConfig_InfersDiscoveryUrlFromRequest()
	{
		using var server = BuildServer(publicResourceUrl: null);
		using var client = server.CreateClient();

		var response = await client.GetAsync("/protected");

		var header = response.Headers.GetValues("WWW-Authenticate").Single();
		header.Should().Contain("resource_metadata=\"http://");
		header.Should().Contain("/.well-known/oauth-protected-resource\"");
	}

	[Fact]
	public async Task AnonymousRequest_AdvertisesRealmFromConfiguration()
	{
		using var server = BuildServer(
			publicResourceUrl: "https://yumney-mcp.example.com/mcp",
			extraConfig: new Dictionary<string, string?> { ["Keycloak:Realm"] = "yumney-staging" });
		using var client = server.CreateClient();

		var response = await client.GetAsync("/protected");

		var header = response.Headers.GetValues("WWW-Authenticate").Single();
		header.Should().Contain("realm=\"yumney-staging\"");
	}

	[Fact]
	public async Task RequestWithInvalidBearer_AdvertisesInvalidTokenError()
	{
		using var server = BuildServer(publicResourceUrl: "https://yumney-mcp.example.com/mcp");
		using var client = server.CreateClient();
		client.DefaultRequestHeaders.Authorization = new("Bearer", "not.a.real.jwt");

		var response = await client.GetAsync("/protected");

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
		var header = response.Headers.GetValues("WWW-Authenticate").Single();
		header.Should().Contain("error=\"invalid_token\"");
	}

	[Fact]
	public async Task AnonymousRequest_ToUnprotectedRoute_DoesNotChallenge()
	{
		using var server = BuildServer(publicResourceUrl: "https://yumney-mcp.example.com/mcp");
		using var client = server.CreateClient();

		var response = await client.GetAsync("/open");

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		response.Headers.Contains("WWW-Authenticate").Should().BeFalse();
	}

	private static TestServer BuildServer(string? publicResourceUrl, IDictionary<string, string?>? extraConfig = null)
	{
		var config = new Dictionary<string, string?>
		{
			["ConnectionStrings:keycloak"] = "http://keycloak.invalid",
		};
		if (publicResourceUrl is not null)
		{
			config["McpServer:PublicUrl"] = publicResourceUrl;
		}

		if (extraConfig is not null)
		{
			foreach (var (key, value) in extraConfig)
			{
				config[key] = value;
			}
		}

		var builder = new HostBuilder()
			.UseEnvironment("Development")
			.ConfigureWebHost(webHost =>
			{
				webHost.UseTestServer();
				webHost.ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(config));
				webHost.ConfigureServices((context, services) =>
				{
					services.AddRouting();
					services.AddKeycloakBearerAuthentication(context.Configuration, context.HostingEnvironment);
				});
				webHost.Configure(app =>
				{
					app.UseRouting();
					app.UseAuthentication();
					app.UseAuthorization();
					app.UseEndpoints(endpoints =>
					{
						endpoints.MapGet("/protected", () => "ok").RequireAuthorization();
						endpoints.MapGet("/open", () => "ok").AllowAnonymous();
					});
				});
			});

		return builder.Start().GetTestServer();
	}
}
