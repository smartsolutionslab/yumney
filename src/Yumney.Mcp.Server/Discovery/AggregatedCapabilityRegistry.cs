using System.Collections.Concurrent;
using SmartSolutionsLab.Yumney.Shared.Capabilities;

namespace SmartSolutionsLab.Yumney.Mcp.Server.Discovery;

/// <summary>
/// Thread-safe singleton holding the union of capability manifests fetched
/// from every known module host. Phase 4b will project this into MCP tool
/// descriptors; Phase 4a only exposes it via a debug endpoint.
/// </summary>
public sealed class AggregatedCapabilityRegistry
{
	private readonly ConcurrentDictionary<string, CapabilityManifest> manifestsByService = new(StringComparer.Ordinal);

	/// <summary>Gets all currently-discovered manifests, keyed by Aspire service name.</summary>
	public IReadOnlyDictionary<string, CapabilityManifest> Manifests => manifestsByService;

	/// <summary>Gets the total count of capabilities across every discovered manifest.</summary>
	public int CapabilityCount => manifestsByService.Values.Sum(manifest => manifest.Capabilities.Count);

	/// <summary>Replace the manifest entry for a service. Called by the discovery service after a successful fetch.</summary>
	/// <param name="serviceName">Aspire service name (e.g. <c>recipes-api</c>).</param>
	/// <param name="manifest">Manifest just fetched from that service.</param>
	public void SetManifest(string serviceName, CapabilityManifest manifest) =>
		manifestsByService[serviceName] = manifest;

	/// <summary>Clear the manifest entry for a service (called when a fetch fails so the registry doesn't serve stale data).</summary>
	/// <param name="serviceName">Aspire service name.</param>
	public void RemoveManifest(string serviceName) => manifestsByService.TryRemove(serviceName, out _);

	/// <summary>Return the union of all discovered capabilities across services.</summary>
	/// <returns>Flat list of every capability descriptor across every discovered service.</returns>
	public IReadOnlyList<CapabilityDescriptor> AllCapabilities() =>
		[.. manifestsByService.Values.SelectMany(manifest => manifest.Capabilities)];
}
