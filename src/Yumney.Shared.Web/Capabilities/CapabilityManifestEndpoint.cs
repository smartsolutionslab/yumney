using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
			var services = context.RequestServices;
			var dataSource = services.GetRequiredService<EndpointDataSource>();
			var apiDescriptions = services.GetService<IApiDescriptionGroupCollectionProvider>()?.ApiDescriptionGroups.Items
				.SelectMany(group => group.Items)
				.ToList() ?? [];
			var serializerOptions = services.GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions;
			var manifest = BuildManifest(serviceName, dataSource, apiDescriptions, serializerOptions);
			return Results.Ok(manifest);
		})
		.AllowAnonymous()
		.WithName("GetCapabilityManifest")
		.WithTags("Capabilities");
		return app;
	}

	internal static CapabilityManifest BuildManifest(
		string serviceName,
		EndpointDataSource dataSource,
		IReadOnlyList<ApiDescription> apiDescriptions,
		JsonSerializerOptions serializerOptions)
	{
		var descriptionsByCapability = IndexByCapability(apiDescriptions);
		var descriptors = dataSource.Endpoints
			.OfType<RouteEndpoint>()
			.Select(endpoint => ProjectDescriptor(endpoint, descriptionsByCapability, serializerOptions))
			.OfType<CapabilityDescriptor>()
			.ToList();
		return new CapabilityManifest(serviceName, descriptors);
	}

	// ApiExplorer copies the endpoint's metadata instances verbatim, so the
	// CapabilityMetadata object itself is the join key between the routing
	// endpoint and its parameter description.
	private static Dictionary<CapabilityMetadata, ApiDescription> IndexByCapability(IReadOnlyList<ApiDescription> apiDescriptions)
	{
		Dictionary<CapabilityMetadata, ApiDescription> index = new(ReferenceEqualityComparer.Instance);
		foreach (var description in apiDescriptions)
		{
			var capability = description.ActionDescriptor.EndpointMetadata.OfType<CapabilityMetadata>().FirstOrDefault();
			if (capability is not null) index.TryAdd(capability, description);
		}

		return index;
	}

	private static CapabilityDescriptor? ProjectDescriptor(
		RouteEndpoint endpoint,
		Dictionary<CapabilityMetadata, ApiDescription> descriptionsByCapability,
		JsonSerializerOptions serializerOptions)
	{
		var meta = endpoint.Metadata.GetMetadata<CapabilityMetadata>();
		if (meta is null) return null;

		var methods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods;
		var httpMethod = methods is { Count: > 0 } ? methods[0] : "GET";
		var routePattern = endpoint.RoutePattern.RawText ?? string.Empty;
		var inputSchema = descriptionsByCapability.TryGetValue(meta, out var description)
			? CapabilityInputSchemaBuilder.Build(description, serializerOptions)
			: (JsonElement?)null;

		return new CapabilityDescriptor(meta.Name, meta.Description, meta.Surfaces, httpMethod, routePattern)
		{
			Version = meta.Version,
			InputSchema = inputSchema,
		};
	}
}
