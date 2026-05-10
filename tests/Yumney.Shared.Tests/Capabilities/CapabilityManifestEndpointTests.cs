using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Primitives;
using SmartSolutionsLab.Yumney.Shared.Capabilities;
using SmartSolutionsLab.Yumney.Shared.Web.Capabilities;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Capabilities;

public class CapabilityManifestEndpointTests
{
	[Fact]
	public void BuildManifest_OnlyTaggedEndpoints_AppearInDescriptors()
	{
		var dataSource = new TestEndpointDataSource(
			TaggedEndpoint("/api/v1/recipes", "GET", "search_recipes", "search the user's recipes", CapabilitySurface.All),
			TaggedEndpoint("/api/v1/recipes/{id}", "GET", "get_recipe", "fetch one recipe", CapabilitySurface.Chat | CapabilitySurface.Mcp),
			UntaggedEndpoint("/health", "GET"));

		var manifest = CapabilityManifestEndpoint.BuildManifest("recipes-api", dataSource);

		manifest.ServiceName.Should().Be("recipes-api");
		manifest.Capabilities.Should().HaveCount(2);
		manifest.Capabilities.Select(capability => capability.Name).Should().Contain(["search_recipes", "get_recipe"]);
		manifest.Capabilities.Select(capability => capability.RoutePattern).Should().NotContain("/health");
	}

	[Fact]
	public void BuildManifest_ProjectsHttpMethodFromMetadata()
	{
		var dataSource = new TestEndpointDataSource(
			TaggedEndpoint("/post-route", "POST", "post_tool", "creates a thing", CapabilitySurface.Mcp),
			TaggedEndpoint("/put-route", "PUT", "put_tool", "updates a thing", CapabilitySurface.Mcp));

		var manifest = CapabilityManifestEndpoint.BuildManifest("test-api", dataSource);

		manifest.Capabilities.Single(capability => capability.Name == "post_tool").HttpMethod.Should().Be("POST");
		manifest.Capabilities.Single(capability => capability.Name == "put_tool").HttpMethod.Should().Be("PUT");
	}

	[Fact]
	public void BuildManifest_PreservesNameDescriptionAndSurfaces()
	{
		var dataSource = new TestEndpointDataSource(
			TaggedEndpoint("/route", "GET", "tool_name", "long description", CapabilitySurface.Chat | CapabilitySurface.Voice));

		var manifest = CapabilityManifestEndpoint.BuildManifest("test-api", dataSource);

		var descriptor = manifest.Capabilities.Single();
		descriptor.Name.Should().Be("tool_name");
		descriptor.Description.Should().Be("long description");
		descriptor.Surfaces.Should().Be(CapabilitySurface.Chat | CapabilitySurface.Voice);
		descriptor.RoutePattern.Should().Be("/route");
	}

	[Fact]
	public void BuildManifest_NoTaggedEndpoints_ReturnsEmptyCapabilityList()
	{
		var dataSource = new TestEndpointDataSource(
			UntaggedEndpoint("/a", "GET"),
			UntaggedEndpoint("/b", "POST"));

		var manifest = CapabilityManifestEndpoint.BuildManifest("test-api", dataSource);

		manifest.ServiceName.Should().Be("test-api");
		manifest.Capabilities.Should().BeEmpty();
	}

	private static RouteEndpoint TaggedEndpoint(string pattern, string method, string name, string description, CapabilitySurface surfaces) =>
		new(
			_ => Task.CompletedTask,
			RoutePatternFactory.Parse(pattern),
			order: 0,
			new EndpointMetadataCollection(
				new HttpMethodMetadata([method]),
				new CapabilityMetadata(name, description, surfaces)),
			displayName: name);

	private static RouteEndpoint UntaggedEndpoint(string pattern, string method) =>
		new(
			_ => Task.CompletedTask,
			RoutePatternFactory.Parse(pattern),
			order: 0,
			new EndpointMetadataCollection(new HttpMethodMetadata([method])),
			displayName: pattern);

	private sealed class TestEndpointDataSource(params Endpoint[] endpoints) : EndpointDataSource
	{
		public override IReadOnlyList<Endpoint> Endpoints { get; } = endpoints;

		public override IChangeToken GetChangeToken() => new CancellationChangeToken(CancellationToken.None);
	}
}
