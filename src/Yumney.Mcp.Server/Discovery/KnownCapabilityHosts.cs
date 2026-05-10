namespace SmartSolutionsLab.Yumney.Mcp.Server.Discovery;

/// <summary>
/// Aspire service names of every module host that publishes a capability
/// manifest. Discovery walks this list at startup. Adding a new module is a
/// one-line change here.
/// </summary>
public static class KnownCapabilityHosts
{
	/// <summary>The Aspire service names this server discovers manifests from.</summary>
	public static readonly IReadOnlyList<string> ServiceNames =
	[
		"recipes-api",
		"mealplan-api",
		"shopping-api",
	];
}
