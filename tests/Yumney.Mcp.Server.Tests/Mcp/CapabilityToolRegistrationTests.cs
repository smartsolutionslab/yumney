using FluentAssertions;
using ModelContextProtocol.Protocol;
using SmartSolutionsLab.Yumney.Mcp.Server.Discovery;
using SmartSolutionsLab.Yumney.Mcp.Server.Mcp;
using SmartSolutionsLab.Yumney.Shared.Capabilities;
using Xunit;

namespace SmartSolutionsLab.Yumney.Mcp.Server.Tests.Mcp;

public class CapabilityToolRegistrationTests
{
	[Fact]
	public void BuildTools_OnlyIncludesCapabilitiesWithMcpSurface()
	{
		var registry = new AggregatedCapabilityRegistry();
		registry.SetManifest("recipes-api", new CapabilityManifest(
			"recipes-api",
			[
			Descriptor("search_recipes", CapabilitySurface.All),
			Descriptor("chat_only_tool", CapabilitySurface.Chat),
			Descriptor("voice_only_tool", CapabilitySurface.Voice),
			Descriptor("mcp_only_tool", CapabilitySurface.Mcp),
		]));

		var tools = CapabilityToolRegistration.BuildTools(registry);

		tools.Should().HaveCount(2);
		tools.Select(tool => tool.Name).Should().BeEquivalentTo(["search_recipes", "mcp_only_tool"]);
	}

	[Fact]
	public void BuildTools_IncludesRoutePatternInDescription()
	{
		var registry = new AggregatedCapabilityRegistry();
		registry.SetManifest("recipes-api", new CapabilityManifest(
			"recipes-api",
			[
			new CapabilityDescriptor(
				"search_recipes",
				"Search the user's recipes",
				CapabilitySurface.All,
				HttpMethod: "GET",
				RoutePattern: "/api/v1/recipes/"),
		]));

		var tool = CapabilityToolRegistration.BuildTools(registry).Single();

		tool.Name.Should().Be("search_recipes");
		tool.Description.Should().Contain("Search the user's recipes");
		tool.Description.Should().Contain("GET /api/v1/recipes/");
	}

	[Fact]
	public void BuildTools_EmptyRegistry_ReturnsEmptyList()
	{
		var registry = new AggregatedCapabilityRegistry();

		var tools = CapabilityToolRegistration.BuildTools(registry);

		tools.Should().BeEmpty();
	}

	[Fact]
	public void BuildTools_NoMcpSurfaceCapabilities_ReturnsEmptyList()
	{
		var registry = new AggregatedCapabilityRegistry();
		registry.SetManifest("recipes-api", new CapabilityManifest(
			"recipes-api",
			[
			Descriptor("chat_only", CapabilitySurface.Chat),
			Descriptor("voice_only", CapabilitySurface.Voice),
		]));

		var tools = CapabilityToolRegistration.BuildTools(registry);

		tools.Should().BeEmpty();
	}

	[Fact]
	public void BuildStubInvocationResult_ReferencesRequestedToolName()
	{
		var result = CapabilityToolRegistration.BuildStubInvocationResult("search_recipes");

		result.IsError.Should().BeFalse();
		result.Content.Should().ContainSingle();
		var text = result.Content.OfType<TextContentBlock>().Single().Text;
		text.Should().Contain("[search_recipes]");
		text.Should().Contain("Phase 4c");
	}

	[Fact]
	public void BuildTools_PopulatesAnnotationsBasedOnHttpMethod()
	{
		var registry = new AggregatedCapabilityRegistry();
		registry.SetManifest("recipes-api", new CapabilityManifest(
			"recipes-api",
			[
			new CapabilityDescriptor("get_recipe", "fetch a recipe", CapabilitySurface.Mcp, "GET", "/recipes/{id}"),
			new CapabilityDescriptor("delete_recipe", "remove a recipe", CapabilitySurface.Mcp, "DELETE", "/recipes/{id}"),
			new CapabilityDescriptor("save_recipe", "import a recipe", CapabilitySurface.Mcp, "POST", "/recipes/"),
		]));

		var tools = CapabilityToolRegistration.BuildTools(registry);

		var getTool = tools.Single(tool => tool.Name == "get_recipe");
		getTool.Annotations!.ReadOnlyHint.Should().BeTrue();
		getTool.Annotations.IdempotentHint.Should().BeTrue();
		getTool.Annotations.DestructiveHint.Should().BeFalse();
		getTool.Annotations.OpenWorldHint.Should().BeFalse();

		var deleteTool = tools.Single(tool => tool.Name == "delete_recipe");
		deleteTool.Annotations!.DestructiveHint.Should().BeTrue();
		deleteTool.Annotations.IdempotentHint.Should().BeTrue();
		deleteTool.Annotations.ReadOnlyHint.Should().BeFalse();

		var postTool = tools.Single(tool => tool.Name == "save_recipe");
		postTool.Annotations!.ReadOnlyHint.Should().BeFalse();
		postTool.Annotations.IdempotentHint.Should().BeFalse();
		postTool.Annotations.DestructiveHint.Should().BeFalse();
	}

	[Fact]
	public void BuildTools_UnionsAcrossManifestsFromMultipleServices()
	{
		var registry = new AggregatedCapabilityRegistry();
		registry.SetManifest("recipes-api", new CapabilityManifest(
			"recipes-api",
			[
			Descriptor("search_recipes", CapabilitySurface.All),
		]));
		registry.SetManifest("shopping-api", new CapabilityManifest(
			"shopping-api",
			[
			Descriptor("get_merged_shopping_list", CapabilitySurface.Mcp),
		]));

		var tools = CapabilityToolRegistration.BuildTools(registry);

		tools.Select(tool => tool.Name).Should().BeEquivalentTo(["search_recipes", "get_merged_shopping_list"]);
	}

	private static CapabilityDescriptor Descriptor(string name, CapabilitySurface surfaces) =>
		new(name, $"description for {name}", surfaces, "GET", $"/{name}");
}
