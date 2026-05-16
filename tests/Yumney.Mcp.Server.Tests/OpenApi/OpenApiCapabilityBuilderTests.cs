using System.Text.Json.Nodes;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Mcp.Server.Discovery;
using SmartSolutionsLab.Yumney.Mcp.Server.OpenApi;
using SmartSolutionsLab.Yumney.Mcp.Server.Tests.OpenApi.TestSupport;
using SmartSolutionsLab.Yumney.Shared.Capabilities;
using Xunit;

namespace SmartSolutionsLab.Yumney.Mcp.Server.Tests.OpenApi;

public class OpenApiCapabilityBuilderTests
{
	[Fact]
	public void Build_OnlyIncludesMcpSurfaceCapabilities()
	{
		var registry = new AggregatedCapabilityRegistry();
		registry.SetManifest("recipes-api", new CapabilityManifest(
			"recipes-api",
			[
				new CapabilityDescriptor("mcp_tool", "MCP", CapabilitySurface.Mcp, "GET", "/api/v1/recipes/"),
				new CapabilityDescriptor("chat_only_tool", "chat", CapabilitySurface.Chat, "GET", "/api/v1/chat-only/"),
			]));

		var spec = OpenApiCapabilityBuilder.Build(registry, "https://gateway.example.com", "https://kc/auth", "https://kc/token");

		var paths = spec["paths"]!.AsObject().AsDictionary();
		paths.Should().HaveCount(1);
		paths.Should().ContainKey("/api/v1/recipes/");
	}

	[Fact]
	public void Build_StripsRouteConstraintsFromPath()
	{
		var registry = new AggregatedCapabilityRegistry();
		registry.SetManifest("recipes-api", new CapabilityManifest(
			"recipes-api",
			[
				new CapabilityDescriptor("get_recipe", "Get recipe by id", CapabilitySurface.Mcp, "GET", "/api/v1/recipes/{identifier:guid}"),
			]));

		var spec = OpenApiCapabilityBuilder.Build(registry, "https://gateway.example.com", "https://kc/auth", "https://kc/token");

		spec["paths"]!.AsObject().AsDictionary().Should().ContainKey("/api/v1/recipes/{identifier}");
	}

	[Fact]
	public void Build_GetOperation_HasNoRequestBody()
	{
		var registry = new AggregatedCapabilityRegistry();
		registry.SetManifest("recipes-api", new CapabilityManifest(
			"recipes-api",
			[new CapabilityDescriptor("search_recipes", "Search", CapabilitySurface.Mcp, "GET", "/api/v1/recipes/")]));

		var spec = OpenApiCapabilityBuilder.Build(registry, "https://g", "https://kc/auth", "https://kc/token");

		var op = spec["paths"]!["/api/v1/recipes/"]!["get"]!.AsObject();
		op.AsDictionary().Should().NotContainKey("requestBody");
	}

	[Theory]
	[InlineData("POST")]
	[InlineData("PUT")]
	[InlineData("PATCH")]
	public void Build_NonGetOperation_HasPermissiveRequestBody(string httpMethod)
	{
		var registry = new AggregatedCapabilityRegistry();
		registry.SetManifest("recipes-api", new CapabilityManifest(
			"recipes-api",
			[new CapabilityDescriptor("write_tool", "Write something", CapabilitySurface.Mcp, httpMethod, "/api/v1/recipes/")]));

		var spec = OpenApiCapabilityBuilder.Build(registry, "https://g", "https://kc/auth", "https://kc/token");

		var op = spec["paths"]!["/api/v1/recipes/"]![httpMethod.ToLowerInvariant()]!.AsObject();
		op.AsDictionary().Should().ContainKey("requestBody");
		op["requestBody"]!["content"]!["application/json"]!["schema"]!["additionalProperties"]!.GetValue<bool>().Should().BeTrue();
	}

	[Fact]
	public void Build_PathParameters_AreEmittedForEachPlaceholder()
	{
		var registry = new AggregatedCapabilityRegistry();
		registry.SetManifest("meal-plan-api", new CapabilityManifest(
			"meal-plan-api",
			[new CapabilityDescriptor("confirm_meal_cooked", "Confirm cooked", CapabilitySurface.Mcp, "POST", "/api/v1/meal-plans/{year:int}/{week:int}/meals/{day}/{mealType}")]));

		var spec = OpenApiCapabilityBuilder.Build(registry, "https://g", "https://kc/auth", "https://kc/token");

		var path = spec["paths"]!["/api/v1/meal-plans/{year}/{week}/meals/{day}/{mealType}"]!.AsObject();
		var parameters = path["post"]!["parameters"]!.AsArray();
		parameters.Should().HaveCount(4);
		parameters.Select(parameter => parameter!["name"]!.GetValue<string>())
			.Should().BeEquivalentTo(["year", "week", "day", "mealType"]);
	}

	[Fact]
	public void Build_SecuritySchemeIsOauth2WithKeycloakUrls()
	{
		var registry = new AggregatedCapabilityRegistry();

		var spec = OpenApiCapabilityBuilder.Build(registry, "https://g", "https://kc.example.com/realms/yumney/protocol/openid-connect/auth", "https://kc.example.com/realms/yumney/protocol/openid-connect/token");

		var scheme = spec["components"]!["securitySchemes"]!["keycloak"]!.AsObject();
		scheme["type"]!.GetValue<string>().Should().Be("oauth2");
		var flow = scheme["flows"]!["authorizationCode"]!.AsObject();
		flow["authorizationUrl"]!.GetValue<string>().Should().Be("https://kc.example.com/realms/yumney/protocol/openid-connect/auth");
		flow["tokenUrl"]!.GetValue<string>().Should().Be("https://kc.example.com/realms/yumney/protocol/openid-connect/token");
		flow["scopes"]!.AsObject().AsDictionary().Should().ContainKey("yumney-api");
	}

	[Fact]
	public void Build_DeclaresOpenApi310()
	{
		var spec = OpenApiCapabilityBuilder.Build(new AggregatedCapabilityRegistry(), "https://g", "https://kc/auth", "https://kc/token");

		spec["openapi"]!.GetValue<string>().Should().Be("3.1.0");
	}

	[Fact]
	public void Build_TopLevelSecurityListsAllRequiredScopes()
	{
		var spec = OpenApiCapabilityBuilder.Build(new AggregatedCapabilityRegistry(), "https://g", "https://kc/auth", "https://kc/token");

		var security = spec["security"]!.AsArray();
		security.Should().HaveCount(1);
		var scopes = security[0]!["keycloak"]!.AsArray();
		scopes.Select(node => node!.GetValue<string>()).Should().Contain("yumney-api");
	}

	[Fact]
	public void Build_MultipleMcpCapabilitiesOnSamePath_ShareThePathItem()
	{
		var registry = new AggregatedCapabilityRegistry();
		registry.SetManifest("recipes-api", new CapabilityManifest(
			"recipes-api",
			[
				new CapabilityDescriptor("search_recipes", "search", CapabilitySurface.Mcp, "GET", "/api/v1/recipes/"),
				new CapabilityDescriptor("save_recipe", "save", CapabilitySurface.Mcp, "POST", "/api/v1/recipes/"),
			]));

		var spec = OpenApiCapabilityBuilder.Build(registry, "https://g", "https://kc/auth", "https://kc/token");

		var pathItem = spec["paths"]!["/api/v1/recipes/"]!.AsObject();
		pathItem.AsDictionary().Should().ContainKey("get");
		pathItem.AsDictionary().Should().ContainKey("post");
	}
}
