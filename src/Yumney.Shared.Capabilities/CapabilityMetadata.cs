namespace SmartSolutionsLab.Yumney.Shared.Capabilities;

/// <summary>
/// Endpoint metadata marking a route as an LLM-callable capability. Attached
/// via <c>WithCapability(...)</c>; surfaced by <c>MapCapabilityManifest()</c>
/// on the well-known manifest endpoint.
/// </summary>
/// <param name="Name">Stable LLM-facing tool name (snake_case). Treat as a public contract — renaming breaks installed clients.</param>
/// <param name="Description">One-paragraph description used by the LLM to decide when to call this capability.</param>
/// <param name="Surfaces">Bitfield of surfaces this capability is exposed on.</param>
public sealed record CapabilityMetadata(string Name, string Description, CapabilitySurface Surfaces);
