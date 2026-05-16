using System.Text.Json.Serialization;

namespace SmartSolutionsLab.Yumney.Mcp.Server.Auth;

/// <summary>
/// RFC 9728 OAuth 2.0 Protected Resource Metadata document. MCP clients
/// (Claude.ai's custom connectors, Claude Desktop's HTTP transport, ChatGPT's
/// native MCP support, etc.) fetch this at
/// <c>/.well-known/oauth-protected-resource</c> to discover the authorization
/// server they should authenticate against — without it, every client has to
/// be told the Keycloak realm URL manually.
/// </summary>
/// <param name="Resource">The canonical URL of the protected resource (the MCP endpoint).</param>
/// <param name="AuthorizationServers">URLs of the authorization servers that issue tokens for this resource.</param>
/// <param name="BearerMethodsSupported">How the bearer is presented; we accept it in the Authorization header only.</param>
/// <param name="ScopesSupported">Scopes a client may request when obtaining a token for this resource.</param>
public sealed record OAuthProtectedResource(
	[property: JsonPropertyName("resource")] string Resource,
	[property: JsonPropertyName("authorization_servers")] IReadOnlyList<string> AuthorizationServers,
	[property: JsonPropertyName("bearer_methods_supported")] IReadOnlyList<string> BearerMethodsSupported,
	[property: JsonPropertyName("scopes_supported")] IReadOnlyList<string> ScopesSupported);
