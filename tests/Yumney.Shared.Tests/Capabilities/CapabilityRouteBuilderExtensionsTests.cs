using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using SmartSolutionsLab.Yumney.Shared.Capabilities;
using SmartSolutionsLab.Yumney.Shared.Web.Capabilities;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Capabilities;

public class CapabilityRouteBuilderExtensionsTests
{
	[Fact]
	public void WithCapability_AttachesCapabilityMetadata()
	{
		var builder = new TestConventionBuilder();

		builder.WithCapability("test_tool", "a test", CapabilitySurface.Chat);

		var metadata = builder.AppliedMetadata.OfType<CapabilityMetadata>().Single();
		metadata.Name.Should().Be("test_tool");
		metadata.Description.Should().Be("a test");
		metadata.Surfaces.Should().Be(CapabilitySurface.Chat);
	}

	[Fact]
	public void WithCapability_DefaultSurfaces_IsChatPlusMcp()
	{
		var builder = new TestConventionBuilder();

		builder.WithCapability("default_tool", "default surfaces");

		var metadata = builder.AppliedMetadata.OfType<CapabilityMetadata>().Single();
		metadata.Surfaces.Should().Be(CapabilitySurface.Chat | CapabilitySurface.Mcp);
	}

	private sealed class TestConventionBuilder : IEndpointConventionBuilder
	{
		public List<object> AppliedMetadata { get; } = [];

		public void Add(Action<EndpointBuilder> convention)
		{
			var endpointBuilder = new TestEndpointBuilder();
			convention(endpointBuilder);
			AppliedMetadata.AddRange(endpointBuilder.Metadata);
		}

		public void Finally(Action<EndpointBuilder> finallyConvention)
		{
		}
	}

	private sealed class TestEndpointBuilder : EndpointBuilder
	{
		public override Endpoint Build() => throw new NotSupportedException();
	}
}
