using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using SmartSolutionsLab.Yumney.Mcp.Server.Discovery;
using SmartSolutionsLab.Yumney.Shared.Capabilities;

namespace SmartSolutionsLab.Yumney.Mcp.Server.Mcp;

/// <summary>
/// Translates discovered <see cref="CapabilityDescriptor"/>s into MCP
/// <see cref="Tool"/> descriptors and routes <see cref="CallToolRequestParams"/>
/// invocations. Phase 4b returns a stub message for invocations because the
/// REST proxy + Keycloak OAuth bridge land in Phase 4c. List-tools is fully
/// functional so external MCP clients (Claude Desktop, custom GPTs) can
/// already enumerate the surface and verify the connection.
/// </summary>
#pragma warning disable SA1303
public static class CapabilityToolRegistration
{
	private const string mcpStubInvocationMessage =
		"The Yumney MCP server is in setup. Tool listing is live, but tool invocation "
		+ "lands with the Keycloak OAuth bridge + REST proxy in Phase 4c (#641). "
		+ "Track progress at https://github.com/smartsolutionslab/yumney/issues/641.";

	/// <summary>List the discovered capabilities exposed on the <c>Mcp</c> surface as MCP tools.</summary>
	/// <param name="registry">Registry populated by <see cref="CapabilityDiscoveryService"/>.</param>
	/// <returns>The current MCP-surface tools, one per capability with surface flag <c>Mcp</c>.</returns>
	public static IReadOnlyList<Tool> BuildTools(AggregatedCapabilityRegistry registry) =>
		[.. registry.AllCapabilities()
			.Where(capability => capability.Surfaces.HasFlag(CapabilitySurface.Mcp))
			.Select(ToTool)];

	/// <summary>Stub call-tool result returned in Phase 4b until the REST proxy ships.</summary>
	/// <param name="toolName">Name the caller asked to invoke.</param>
	/// <returns>A non-error tool result with the stub message.</returns>
	public static CallToolResult BuildStubInvocationResult(string toolName) =>
		new()
		{
			Content = [new TextContentBlock { Text = $"[{toolName}] {mcpStubInvocationMessage}" }],
			IsError = false,
		};

	/// <summary>The MCP <c>tools/list</c> handler — projects the registry into the protocol shape.</summary>
	/// <param name="context">Request context (unused — we list everything).</param>
	/// <param name="cancellationToken">Cancellation propagated from the SDK.</param>
	/// <returns>The current MCP-surface tools.</returns>
	public static ValueTask<ListToolsResult> ListToolsAsync(RequestContext<ListToolsRequestParams> context, CancellationToken cancellationToken)
	{
		var registry = context.Services!.GetRequiredService<AggregatedCapabilityRegistry>();
		return ValueTask.FromResult(new ListToolsResult { Tools = [.. BuildTools(registry)] });
	}

	/// <summary>The MCP <c>tools/call</c> handler — Phase 4b stub; Phase 4c proxies to module REST.</summary>
	/// <param name="context">Request context with the called tool's name + arguments.</param>
	/// <param name="cancellationToken">Cancellation propagated from the SDK.</param>
	/// <returns>The stub result.</returns>
	public static ValueTask<CallToolResult> CallToolAsync(RequestContext<CallToolRequestParams> context, CancellationToken cancellationToken)
	{
		var name = context.Params?.Name ?? "<unknown>";
		return ValueTask.FromResult(BuildStubInvocationResult(name));
	}

	private static Tool ToTool(CapabilityDescriptor capability) => new()
	{
		Name = capability.Name,
		Description = $"{capability.Description} (proxies {capability.HttpMethod} {capability.RoutePattern})",
	};
}
