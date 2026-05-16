using ModelContextProtocol.Protocol;

namespace SmartSolutionsLab.Yumney.Mcp.Server.Mcp;

/// <summary>
/// Derives MCP <see cref="ToolAnnotations"/> from the HTTP method the
/// capability proxies. The hints let MCP clients (Claude Desktop, custom
/// GPTs) skip confirmation for read-only tools, always confirm destructive
/// ones, and cache results from idempotent calls.
/// </summary>
public static class ToolAnnotationsBuilder
{
	/// <summary>
	/// Maps an HTTP method to its annotation profile. <c>OpenWorldHint</c> is
	/// always false because Yumney tools only touch our own modules — from
	/// the LLM's perspective there's no external-world reach.
	/// </summary>
	/// <remarks>
	/// HTTP semantics drive the mapping: GET is read-only; DELETE is
	/// destructive; GET / PUT / DELETE are idempotent per RFC 9110. POST and
	/// PATCH get the conservative default (all hints false).
	/// </remarks>
	/// <param name="httpMethod">HTTP verb (GET, POST, …) — case insensitive.</param>
	/// <returns>Populated annotations.</returns>
	public static ToolAnnotations FromHttpMethod(string httpMethod)
	{
		var normalized = httpMethod.ToUpperInvariant();
		return new ToolAnnotations
		{
			ReadOnlyHint = normalized == "GET",
			DestructiveHint = normalized == "DELETE",
			IdempotentHint = normalized is "GET" or "PUT" or "DELETE",
			OpenWorldHint = false,
		};
	}
}
