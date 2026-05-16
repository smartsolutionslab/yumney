using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Shared.Capabilities;

namespace SmartSolutionsLab.Yumney.Shared.Web.Capabilities;

/// <summary>
/// Maps the well-known capability manifest endpoint that exposes every
/// <see cref="CapabilityMetadata"/>-tagged route on this host.
/// </summary>
public static class CapabilityManifestEndpoint
{
	/// <summary>The well-known path: <c>/.well-known/yumney-capabilities</c>.</summary>
	public const string WellKnownPath = "/.well-known/yumney-capabilities";

	/// <summary>Map the capability manifest endpoint on this host.</summary>
	/// <param name="app">Endpoint route builder.</param>
	/// <param name="serviceName">Aspire service name (e.g. <c>recipes-api</c>) — surfaced in the manifest.</param>
	/// <returns>The same builder, for fluent chaining.</returns>
	public static IEndpointRouteBuilder MapCapabilityManifest(this IEndpointRouteBuilder app, string serviceName)
	{
		app.MapGet(WellKnownPath, (HttpContext context) =>
		{
			var dataSource = context.RequestServices.GetRequiredService<EndpointDataSource>();
			var manifest = BuildManifest(serviceName, dataSource);
			return Results.Ok(manifest);
		})
		.AllowAnonymous()
		.WithName("GetCapabilityManifest")
		.WithTags("Capabilities");
		return app;
	}

	internal static CapabilityManifest BuildManifest(string serviceName, EndpointDataSource dataSource)
	{
		var descriptors = dataSource.Endpoints
			.OfType<RouteEndpoint>()
			.Select(ProjectDescriptor)
			.OfType<CapabilityDescriptor>()
			.ToList();
		return new CapabilityManifest(serviceName, descriptors);
	}

	private static CapabilityDescriptor? ProjectDescriptor(RouteEndpoint endpoint)
	{
		var meta = endpoint.Metadata.GetMetadata<CapabilityMetadata>();
		if (meta is null) return null;

		var methods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods;
		var httpMethod = methods is { Count: > 0 } ? methods[0] : "GET";
		var routePattern = endpoint.RoutePattern.RawText ?? string.Empty;

		return new CapabilityDescriptor(meta.Name, meta.Description, meta.Surfaces, httpMethod, routePattern) { Version = meta.Version };
	}
}
