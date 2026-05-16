namespace SmartSolutionsLab.Yumney.Shared.Capabilities;

/// <summary>
/// Endpoint metadata marking a route as an LLM-callable capability. Attached
/// via <c>WithCapability(...)</c>; surfaced by <c>MapCapabilityManifest()</c>
/// on the well-known manifest endpoint.
/// </summary>
/// <param name="Name">Stable LLM-facing tool name (snake_case). Treat as a public contract — renaming breaks installed clients.</param>
/// <param name="Description">One-paragraph description used by the LLM to decide when to call this capability.</param>
/// <param name="Surfaces">Bitfield of surfaces this capability is exposed on.</param>
public sealed record CapabilityMetadata(string Name, string Description, CapabilitySurface Surfaces)
{
	/// <summary>
	/// Gets the tool version. Defaults to 1; bumped per the rules in
	/// <c>docs/runbooks/mcp-versioning.md</c> when a breaking change ships.
	/// Wire name on MCP is <c>{Name}</c> for v1 and <c>{Name}__v{Version}</c>
	/// for v ≥ 2 so old client configs keep working.
	/// </summary>
	public int Version { get; init; } = 1;
}
