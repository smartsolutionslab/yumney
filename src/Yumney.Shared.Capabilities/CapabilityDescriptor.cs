namespace SmartSolutionsLab.Yumney.Shared.Capabilities;

/// <summary>
/// Wire shape of one capability — the projection of <see cref="CapabilityMetadata"/>
/// plus the route the capability is mounted on.
/// </summary>
/// <param name="Name">Stable LLM-facing tool name.</param>
/// <param name="Description">One-paragraph description for the LLM.</param>
/// <param name="Surfaces">Bitfield of surfaces this capability is exposed on.</param>
/// <param name="HttpMethod">HTTP verb (GET, POST, PUT, DELETE, …).</param>
/// <param name="RoutePattern">Raw route pattern (e.g. <c>/api/v1/recipes/{id:guid}</c>).</param>
public sealed record CapabilityDescriptor(
	string Name,
	string Description,
	CapabilitySurface Surfaces,
	string HttpMethod,
	string RoutePattern);

/// <summary>Wire shape of one host's capability manifest, returned by <c>/.well-known/yumney-capabilities</c>.</summary>
/// <param name="ServiceName">Aspire service name (e.g. <c>recipes-api</c>).</param>
/// <param name="Capabilities">All capabilities advertised by this host.</param>
public sealed record CapabilityManifest(string ServiceName, IReadOnlyList<CapabilityDescriptor> Capabilities);
