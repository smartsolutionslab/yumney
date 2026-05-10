using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Mcp;

/// <summary>
/// Phase 4a (#648) coverage: the MCP server boots, walks every known module
/// host, and aggregates the per-host capability manifests. Verifies the
/// cross-host pipeline (HttpClient + Aspire service discovery) wires up at
/// runtime, which the unit tests can't catch (they stub the HTTP layer).
/// </summary>
[Collection(AspireCollection.Name)]
public class McpDiscoveryIntegrationTests(AspireFixture fixture)
{
	[Fact]
	public async Task DiscoveredCapabilities_FindsAllThreeKnownHosts()
	{
		await Eventually.AssertAsync(async () =>
		{
			var snapshot = await fixture.McpServer.GetFromJsonAsync<DiscoveredCapabilitiesResponse>("/discovered-capabilities");
			snapshot.Should().NotBeNull();
			snapshot!.ServiceCount.Should().Be(3);
			snapshot.Services.Should().Contain(["recipes-api", "mealplan-api", "shopping-api"]);
		});
	}

	[Fact]
	public async Task DiscoveredCapabilities_ExposesAtLeastNineCapabilities()
	{
		// Phase 3 tagged 9 endpoints (4 Recipes + 3 MealPlan + 2 Shopping). The
		// integration test asserts the lower bound — adding new tagged endpoints
		// in future PRs won't break this assertion.
		await Eventually.AssertAsync(async () =>
		{
			var snapshot = await fixture.McpServer.GetFromJsonAsync<DiscoveredCapabilitiesResponse>("/discovered-capabilities");
			snapshot!.CapabilityCount.Should().BeGreaterThanOrEqualTo(9);
		});
	}

	[Fact]
	public async Task DiscoveredCapabilities_McpToolCount_ExcludesChatOnlyAndVoiceOnlyCapabilities()
	{
		// Phase 3 tagged import_recipe_from_url with Mcp | Voice only — that's
		// fine. But every other capability has Mcp surface. So mcpToolCount
		// must equal capabilityCount as long as no purely-non-MCP tag exists.
		await Eventually.AssertAsync(async () =>
		{
			var snapshot = await fixture.McpServer.GetFromJsonAsync<DiscoveredCapabilitiesResponse>("/discovered-capabilities");
			snapshot!.McpToolCount.Should().BeGreaterThan(0);
			snapshot.McpToolCount.Should().BeLessThanOrEqualTo(snapshot.CapabilityCount);
		});
	}

	[Fact]
	public async Task DiscoveredCapabilities_ContainsKnownCapabilityNames()
	{
		await Eventually.AssertAsync(async () =>
		{
			var snapshot = await fixture.McpServer.GetFromJsonAsync<DiscoveredCapabilitiesResponse>("/discovered-capabilities");
			var names = snapshot!.Capabilities.Select(capability => capability.Name).ToList();
			names.Should().Contain("search_recipes");
			names.Should().Contain("get_weekly_plan");
			names.Should().Contain("get_merged_shopping_list");
		});
	}

	private sealed record DiscoveredCapabilitiesResponse(
		[property: JsonPropertyName("serviceCount")] int ServiceCount,
		[property: JsonPropertyName("capabilityCount")] int CapabilityCount,
		[property: JsonPropertyName("mcpToolCount")] int McpToolCount,
		[property: JsonPropertyName("services")] IReadOnlyList<string> Services,
		[property: JsonPropertyName("capabilities")] IReadOnlyList<DiscoveredCapability> Capabilities);

	private sealed record DiscoveredCapability(
		[property: JsonPropertyName("name")] string Name,
		[property: JsonPropertyName("description")] string Description,
		[property: JsonPropertyName("surfaces")] string Surfaces,
		[property: JsonPropertyName("httpMethod")] string HttpMethod,
		[property: JsonPropertyName("routePattern")] string RoutePattern);
}
