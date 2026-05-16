using System.Net;
using System.Net.Http.Json;
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

public class OAuthProtectedResourceEndpointHttpTests
{
	private const string DiscoveryPath = "/.well-known/oauth-protected-resource";

	[Fact]
	public async Task GET_Discovery_AllowsAnonymousAccess()
	{
		using var server = BuildServer();
		using var client = server.CreateClient();

		var response = await client.GetAsync(DiscoveryPath);

		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task GET_Discovery_SetsFiveMinuteCacheControl()
	{
		using var server = BuildServer();
		using var client = server.CreateClient();

		var response = await client.GetAsync(DiscoveryPath);

		response.Headers.CacheControl.Should().NotBeNull();
		response.Headers.CacheControl!.Public.Should().BeTrue();
		response.Headers.CacheControl.MaxAge.Should().Be(TimeSpan.FromSeconds(300));
	}

	[Fact]
	public async Task GET_Discovery_WithoutOverride_UsesRequestSchemeAndHost()
	{
		using var server = BuildServer(configuredResourceUrl: null);
		using var client = server.CreateClient();

		var doc = await client.GetFromJsonAsync<OAuthProtectedResource>(DiscoveryPath);

		doc!.Resource.Should().EndWith("/mcp");
		doc.Resource.Should().StartWith("http://");
	}

	[Fact]
	public async Task GET_Discovery_WithOverride_HonorsConfiguredResourceUrl()
	{
		const string publicUrl = "https://yumney-mcp.example.com/mcp";
		using var server = BuildServer(configuredResourceUrl: publicUrl);
		using var client = server.CreateClient();

		var doc = await client.GetFromJsonAsync<OAuthProtectedResource>(DiscoveryPath);

		doc!.Resource.Should().Be(publicUrl);
	}

	[Fact]
	public async Task GET_Discovery_AdvertisesRealmUrlFromConfiguration()
	{
		var configOverrides = new Dictionary<string, string?>
		{
			["ConnectionStrings:keycloak"] = "https://yumney-keycloak.example.com",
			["Keycloak:Realm"] = "yumney-staging",
		};
		using var server = BuildServer(configOverrides: configOverrides);
		using var client = server.CreateClient();

		var doc = await client.GetFromJsonAsync<OAuthProtectedResource>(DiscoveryPath);

		doc!.AuthorizationServers.Should().ContainSingle()
			.Which.Should().Be("https://yumney-keycloak.example.com/realms/yumney-staging");
	}

	[Fact]
	public async Task GET_Discovery_FallsBackToDefaultRealmWhenUnconfigured()
	{
		using var server = BuildServer();
		using var client = server.CreateClient();

		var doc = await client.GetFromJsonAsync<OAuthProtectedResource>(DiscoveryPath);

		doc!.AuthorizationServers.Should().ContainSingle()
			.Which.Should().Be("http://localhost:8080/realms/yumney");
	}

	private static TestServer BuildServer(
		string? configuredResourceUrl = null,
		IDictionary<string, string?>? configOverrides = null)
	{
		var builder = new HostBuilder().ConfigureWebHost(webHost =>
		{
			webHost.UseTestServer();
			webHost.ConfigureAppConfiguration(config =>
			{
				if (configOverrides is not null)
				{
					config.AddInMemoryCollection(configOverrides);
				}
			});
			webHost.ConfigureServices(services => services.AddRouting());
			webHost.Configure(app =>
			{
				app.UseRouting();
				app.UseEndpoints(endpoints =>
				{
					endpoints.MapOAuthProtectedResourceEndpoint(configuredResourceUrl);
				});
			});
		});

		var host = builder.Start();
		return host.GetTestServer();
	}
}
