using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Shared.Capabilities;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Capabilities;

/// <summary>
/// Phase 3 (#647) coverage: each module's well-known capability manifest endpoint
/// returns the [WithCapability]-tagged routes. Catches drift between source-tagging
/// and what the live host actually advertises.
/// </summary>
[Collection(AspireCollection.Name)]
public class CapabilityManifestEndpointIntegrationTests(AspireFixture fixture)
{
	private const string WellKnownPath = "/.well-known/yumney-capabilities";

	[Fact]
	public async Task RecipesManifest_ContainsExpectedTaggedEndpoints()
	{
		var manifest = await fixture.RecipesApi.GetFromJsonAsync<CapabilityManifest>(WellKnownPath, AspireFixture.JsonOptions);

		manifest.Should().NotBeNull();
		manifest!.ServiceName.Should().Be("recipes-api");
		manifest.Capabilities.Select(capability => capability.Name).Should().Contain([
			"search_recipes",
			"get_recipe",
			"get_cookable_recipes",
			"import_recipe_from_url",
		]);
	}

	[Fact]
	public async Task MealPlanManifest_ContainsExpectedTaggedEndpoints()
	{
		var manifest = await fixture.MealPlanApi.GetFromJsonAsync<CapabilityManifest>(WellKnownPath, AspireFixture.JsonOptions);

		manifest.Should().NotBeNull();
		manifest!.ServiceName.Should().Be("mealplan-api");
		manifest.Capabilities.Select(capability => capability.Name).Should().Contain([
			"get_weekly_plan",
			"assign_meal",
			"confirm_meal_cooked",
		]);
	}

	[Fact]
	public async Task ShoppingManifest_ContainsExpectedTaggedEndpoints()
	{
		var manifest = await fixture.ShoppingApi.GetFromJsonAsync<CapabilityManifest>(WellKnownPath, AspireFixture.JsonOptions);

		manifest.Should().NotBeNull();
		manifest!.ServiceName.Should().Be("shopping-api");
		manifest.Capabilities.Select(capability => capability.Name).Should().Contain([
			"get_merged_shopping_list",
			"create_shopping_list_from_recipes",
		]);
	}

	[Fact]
	public async Task ManifestEndpoint_IsAnonymous_NoBearerNeeded()
	{
		fixture.RecipesApi.DefaultRequestHeaders.Authorization = null;

		var response = await fixture.RecipesApi.GetAsync(WellKnownPath);

		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task EveryDescriptor_HasMcpOrChatSurface_AndPathStartsWithApiV1()
	{
		var manifest = await fixture.RecipesApi.GetFromJsonAsync<CapabilityManifest>(WellKnownPath, AspireFixture.JsonOptions);

		manifest!.Capabilities.Should().NotBeEmpty();
		manifest.Capabilities.Should().AllSatisfy(capability =>
		{
			(capability.Surfaces & (CapabilitySurface.Mcp | CapabilitySurface.Chat | CapabilitySurface.Voice)).Should().NotBe(CapabilitySurface.None);
			capability.RoutePattern.Should().StartWith("/api/v1/");
		});
	}

	[Fact]
	public async Task SearchRecipesDescriptor_HasGetMethodAndCorrectRoute()
	{
		var manifest = await fixture.RecipesApi.GetFromJsonAsync<CapabilityManifest>(WellKnownPath, AspireFixture.JsonOptions);

		var search = manifest!.Capabilities.Single(capability => capability.Name == "search_recipes");
		search.HttpMethod.Should().Be("GET");
		search.RoutePattern.Should().Be("/api/v1/recipes/");
	}

	[Fact]
	public async Task AssignMealDescriptor_HasPostMethodAndIncludesPlaceholders()
	{
		var manifest = await fixture.MealPlanApi.GetFromJsonAsync<CapabilityManifest>(WellKnownPath, AspireFixture.JsonOptions);

		var assign = manifest!.Capabilities.Single(capability => capability.Name == "assign_meal");
		assign.HttpMethod.Should().Be("POST");
		assign.RoutePattern.Should().Contain("{year:int}");
		assign.RoutePattern.Should().Contain("{weekNumber:int}");
		assign.RoutePattern.Should().EndWith("/slots");
	}
}
