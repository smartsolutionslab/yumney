namespace SmartSolutionsLab.Yumney.Shared.Capabilities;

/// <summary>
/// Flags marking which LLM-callable surfaces an endpoint is exposed on.
/// Combine with bitwise OR. See ADR 0002 for the surface inventory.
/// </summary>
[Flags]
public enum CapabilitySurface
{
	/// <summary>Endpoint is not exposed to any LLM-callable surface.</summary>
	None = 0,

	/// <summary>Exposed to the in-app chat panel (Semantic Kernel function calling).</summary>
	Chat = 1,

	/// <summary>Exposed to external Model Context Protocol clients (Claude Desktop, custom GPTs, …).</summary>
	Mcp = 2,

	/// <summary>Exposed to the future voice-command surface.</summary>
	Voice = 4,

	/// <summary>Convenience union covering every current surface.</summary>
	All = Chat | Mcp | Voice,
}
