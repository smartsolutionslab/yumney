using System.Net.Http.Headers;
using Aspire.Hosting.Testing;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Mcp;

/// <summary>
/// Phase 4 closing test: actually drives the MCP protocol end-to-end.
/// Connects an MCP client (the SDK's <c>HttpClientTransport</c>) to the
/// running mcp-server with a Keycloak bearer, lists tools, and invokes one
/// — exercising the full chain from protocol → CallToolHandler →
/// RestProxyService → real upstream module via Aspire service discovery.
/// </summary>
[Collection(AspireCollection.Name)]
public class McpToolInvocationIntegrationTests(AspireFixture fixture)
{
	[Fact]
	public async Task ListToolsAsync_ReturnsDiscoveredMcpSurfaceTools()
	{
		await using var client = await CreateClientAsync();

		var tools = await client.ListToolsAsync();

		tools.Should().NotBeEmpty();
		tools.Select(tool => tool.Name).Should().Contain([
			"search_recipes",
			"get_recipe",
			"get_cookable_recipes",
			"get_weekly_plan",
			"get_merged_shopping_list",
		]);
	}

	[Fact]
	public async Task CallToolAsync_SearchRecipes_ProxiesToRecipesApiAndReturnsResponse()
	{
		await using var client = await CreateClientAsync();

		var result = await client.CallToolAsync("search_recipes", arguments: new Dictionary<string, object?>());

		// The user has no seeded recipes, so the response is an empty paged
		// result — but it deserialized, which proves the proxy hit the real
		// Recipes API with the bearer and got 200 back.
		result.IsError.Should().BeFalse();
		result.Content.OfType<TextContentBlock>().Should().NotBeEmpty();
		var firstText = result.Content.OfType<TextContentBlock>().First().Text;
		firstText.Should().Contain("\"items\"");
	}

	[Fact]
	public async Task CallToolAsync_GetMergedShoppingList_ProxiesToShoppingApi()
	{
		await using var client = await CreateClientAsync();

		var result = await client.CallToolAsync("get_merged_shopping_list", arguments: new Dictionary<string, object?>());

		result.IsError.Should().BeFalse();
		result.Content.OfType<TextContentBlock>().Should().NotBeEmpty();
		var firstText = result.Content.OfType<TextContentBlock>().First().Text;
		firstText.Should().Contain("\"items\"");
	}

	[Fact]
	public async Task CallToolAsync_GetWeeklyPlan_ProxiesToMealPlanApi()
	{
		await using var client = await CreateClientAsync();

		var result = await client.CallToolAsync("get_weekly_plan", arguments: new Dictionary<string, object?>
		{
			["year"] = 2026,
			["weekNumber"] = 19,
		});

		result.IsError.Should().BeFalse();
		result.Content.OfType<TextContentBlock>().Should().NotBeEmpty();
	}

	[Fact]
	public async Task CallToolAsync_UnknownTool_ReturnsErrorResult()
	{
		await using var client = await CreateClientAsync();

		var result = await client.CallToolAsync("nonexistent_tool", arguments: new Dictionary<string, object?>());

		result.IsError.Should().BeTrue();
	}

	private async Task<McpClient> CreateClientAsync()
	{
		var token = await fixture.GetAccessTokenAsync("testuser", "Test1234");
		var http = fixture.App.CreateHttpClient("mcp-server");
		http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

		var options = new HttpClientTransportOptions
		{
			Endpoint = new Uri(http.BaseAddress!, "/mcp"),
			Name = "mcp-integration-test",
		};
		var transport = new HttpClientTransport(options, http, NullLoggerFactory.Instance, ownsHttpClient: true);

		return await McpClient.CreateAsync(transport, clientOptions: null, loggerFactory: NullLoggerFactory.Instance);
	}
}
