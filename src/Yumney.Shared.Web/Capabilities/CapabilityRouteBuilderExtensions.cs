using Microsoft.AspNetCore.Builder;
using SmartSolutionsLab.Yumney.Shared.Capabilities;

namespace SmartSolutionsLab.Yumney.Shared.Web.Capabilities;

/// <summary>
/// Endpoint-builder extensions for tagging routes with <see cref="CapabilityMetadata"/>.
/// </summary>
public static class CapabilityRouteBuilderExtensions
{
	/// <summary>Mark an endpoint as an LLM-callable capability.</summary>
	/// <typeparam name="TBuilder">Concrete endpoint convention builder type.</typeparam>
	/// <param name="builder">The endpoint builder returned by <c>MapGet</c> / <c>MapPost</c> / etc.</param>
	/// <param name="name">Stable LLM-facing tool name (snake_case).</param>
	/// <param name="description">One-paragraph description used by the LLM.</param>
	/// <param name="surfaces">Surfaces this capability is exposed on. Defaults to Chat | Mcp.</param>
	/// <param name="version">Tool version. Default 1; bump per <c>docs/runbooks/mcp-versioning.md</c> on a breaking change.</param>
	/// <returns>The same builder, for fluent chaining.</returns>
	public static TBuilder WithCapability<TBuilder>(
		this TBuilder builder,
		string name,
		string description,
		CapabilitySurface surfaces = CapabilitySurface.Chat | CapabilitySurface.Mcp,
		int version = 1)
		where TBuilder : IEndpointConventionBuilder =>
		builder.WithMetadata(new CapabilityMetadata(name, description, surfaces) { Version = version });
}
