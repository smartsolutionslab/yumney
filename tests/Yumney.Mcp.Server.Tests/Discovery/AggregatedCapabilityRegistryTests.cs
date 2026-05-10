using FluentAssertions;
using SmartSolutionsLab.Yumney.Mcp.Server.Discovery;
using SmartSolutionsLab.Yumney.Shared.Capabilities;
using Xunit;

namespace SmartSolutionsLab.Yumney.Mcp.Server.Tests.Discovery;

public class AggregatedCapabilityRegistryTests
{
	[Fact]
	public void SetManifest_NewService_AddsManifest()
	{
		var registry = new AggregatedCapabilityRegistry();
		var manifest = new CapabilityManifest("recipes-api", [
			Descriptor("search_recipes", "GET", "/recipes"),
			Descriptor("get_recipe", "GET", "/recipes/{id}"),
		]);

		registry.SetManifest("recipes-api", manifest);

		registry.Manifests.Should().ContainKey("recipes-api");
		registry.Manifests["recipes-api"].Should().BeSameAs(manifest);
		registry.CapabilityCount.Should().Be(2);
	}

	[Fact]
	public void SetManifest_OverwritesExistingManifestForSameService()
	{
		var registry = new AggregatedCapabilityRegistry();
		registry.SetManifest("recipes-api", new CapabilityManifest("recipes-api", [Descriptor("a", "GET", "/a")]));
		var newer = new CapabilityManifest("recipes-api", [
			Descriptor("a", "GET", "/a"),
			Descriptor("b", "POST", "/b"),
		]);

		registry.SetManifest("recipes-api", newer);

		registry.Manifests.Should().HaveCount(1);
		registry.CapabilityCount.Should().Be(2);
	}

	[Fact]
	public void RemoveManifest_DropsExistingService()
	{
		var registry = new AggregatedCapabilityRegistry();
		registry.SetManifest("recipes-api", new CapabilityManifest("recipes-api", [Descriptor("a", "GET", "/a")]));

		registry.RemoveManifest("recipes-api");

		registry.Manifests.Should().BeEmpty();
		registry.CapabilityCount.Should().Be(0);
	}

	[Fact]
	public void RemoveManifest_UnknownService_IsNoOp()
	{
		var registry = new AggregatedCapabilityRegistry();
		registry.SetManifest("recipes-api", new CapabilityManifest("recipes-api", [Descriptor("a", "GET", "/a")]));

		registry.RemoveManifest("not-here");

		registry.Manifests.Should().HaveCount(1);
	}

	[Fact]
	public void AllCapabilities_ReturnsUnionAcrossServices()
	{
		var registry = new AggregatedCapabilityRegistry();
		registry.SetManifest("recipes-api", new CapabilityManifest("recipes-api", [
			Descriptor("search_recipes", "GET", "/recipes"),
			Descriptor("get_recipe", "GET", "/recipes/{id}"),
		]));
		registry.SetManifest("shopping-api", new CapabilityManifest("shopping-api", [
			Descriptor("get_merged_shopping_list", "GET", "/shopping-lists/merged"),
		]));

		var all = registry.AllCapabilities();

		all.Should().HaveCount(3);
		all.Select(descriptor => descriptor.Name).Should().Contain(["search_recipes", "get_recipe", "get_merged_shopping_list"]);
	}

	private static CapabilityDescriptor Descriptor(string name, string method, string route) =>
		new(name, $"description for {name}", CapabilitySurface.All, method, route);
}
