using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using SmartSolutionsLab.Yumney.Mcp.Server.Discovery;
using SmartSolutionsLab.Yumney.Shared.Capabilities;

namespace SmartSolutionsLab.Yumney.Mcp.Server.Mcp;

/// <summary>
/// Translates discovered <see cref="CapabilityDescriptor"/>s into MCP
/// <see cref="Tool"/> descriptors for the <c>tools/list</c> handler.
/// Invocation is handled by <see cref="RestProxyService"/>.
/// </summary>
public static class CapabilityToolRegistration
{
	/// <summary>List the discovered capabilities exposed on the <c>Mcp</c> surface as MCP tools.</summary>
	/// <param name="registry">Registry populated by <see cref="CapabilityDiscoveryService"/>.</param>
	/// <returns>The current MCP-surface tools, one per capability with surface flag <c>Mcp</c>.</returns>
	public static IReadOnlyList<Tool> BuildTools(AggregatedCapabilityRegistry registry) =>
		[.. registry.AllCapabilities()
			.Where(capability => capability.Surfaces.HasFlag(CapabilitySurface.Mcp))
			.Select(ToTool)];

	/// <summary>The MCP <c>tools/list</c> handler — projects the registry into the protocol shape.</summary>
	/// <param name="context">Request context (unused — we list everything).</param>
	/// <param name="cancellationToken">Cancellation propagated from the SDK.</param>
	/// <returns>The current MCP-surface tools.</returns>
	public static ValueTask<ListToolsResult> ListToolsAsync(RequestContext<ListToolsRequestParams> context, CancellationToken cancellationToken)
	{
		var registry = context.Services!.GetRequiredService<AggregatedCapabilityRegistry>();
		return ValueTask.FromResult(new ListToolsResult { Tools = [.. BuildTools(registry)] });
	}

	/// <summary>
	/// Tool name as advertised on the MCP wire. v1 keeps the bare name so the
	/// migration to versioned tools doesn't break installed client configs;
	/// v ≥ 2 suffixes with <c>__v{Version}</c> per <c>docs/runbooks/mcp-versioning.md</c>.
	/// </summary>
	/// <param name="capability">Descriptor whose name + version to project.</param>
	/// <returns>Wire-shape tool name.</returns>
	public static string WireName(CapabilityDescriptor capability) =>
		capability.Version <= 1 ? capability.Name : $"{capability.Name}__v{capability.Version}";

	private static Tool ToTool(CapabilityDescriptor capability)
	{
		var tool = new Tool
		{
			Name = WireName(capability),
			Description = $"{capability.Description} (proxies {capability.HttpMethod} {capability.RoutePattern})",
			Annotations = ToolAnnotationsBuilder.FromHttpMethod(capability.HttpMethod),
		};

		// Without a schema the SDK default (an argument-less object) stays in place.
		if (capability.InputSchema is { } inputSchema)
		{
			tool.InputSchema = inputSchema;
		}

		return tool;
	}
}
