namespace SmartSolutionsLab.Yumney.Mcp.Server.Auth;

/// <summary>
/// Maps the RFC 9728 OAuth Protected Resource Metadata endpoint at
/// <c>/.well-known/oauth-protected-resource</c>. The endpoint is anonymous —
/// discovery must work pre-auth — and returns a 5-minute Cache-Control header.
/// </summary>
public static class OAuthProtectedResourceEndpoint
{
#pragma warning disable SA1303
	private const string discoveryPath = "/.well-known/oauth-protected-resource";
	private const string mcpResourcePath = "/mcp";
	private const int cacheTtlSeconds = 300;
#pragma warning restore SA1303

#pragma warning disable SA1311
	private static readonly string[] bearerMethods = ["header"];
	private static readonly string[] supportedScopes = ["openid", "profile", "yumney-api"];
#pragma warning restore SA1311

	public static IEndpointRouteBuilder MapOAuthProtectedResourceEndpoint(this IEndpointRouteBuilder app) =>
		MapOAuthProtectedResourceEndpoint(app, configuredResourceUrl: null);

	/// <summary>
	/// Maps the discovery endpoint. <paramref name="configuredResourceUrl"/> overrides
	/// the resource URL — set it from configuration when the MCP server sits behind a
	/// gateway (the request's host header is the internal container name, not the
	/// public URL clients use).
	/// </summary>
	/// <param name="app">Route builder.</param>
	/// <param name="configuredResourceUrl">Public MCP URL, or null to infer from the request.</param>
	/// <returns>The route builder for chaining.</returns>
	public static IEndpointRouteBuilder MapOAuthProtectedResourceEndpoint(this IEndpointRouteBuilder app, string? configuredResourceUrl)
	{
		app.MapGet(discoveryPath, (HttpContext context, IConfiguration configuration) =>
		{
			var resourceUrl = configuredResourceUrl ?? InferResourceUrl(context.Request.Scheme, context.Request.Host.ToString());
			var realmUrl = KeycloakAuthExtensions.ResolveRealmUrl(configuration);
			var document = BuildDocument(resourceUrl, realmUrl);

			context.Response.Headers.CacheControl = $"public, max-age={cacheTtlSeconds}";
			return Results.Ok(document);
		}).AllowAnonymous();

		return app;
	}

	/// <summary>Builds the protected-resource document from already-resolved URLs. Exposed for tests.</summary>
	/// <param name="resourceUrl">Canonical public URL of the MCP endpoint.</param>
	/// <param name="authorizationServerUrl">Keycloak realm URL.</param>
	/// <returns>The RFC 9728 document.</returns>
	public static OAuthProtectedResource BuildDocument(string resourceUrl, string authorizationServerUrl) =>
		new(
			Resource: resourceUrl,
			AuthorizationServers: [authorizationServerUrl],
			BearerMethodsSupported: bearerMethods,
			ScopesSupported: supportedScopes);

	/// <summary>Infers the canonical resource URL from the inbound request's scheme + host. Exposed for tests.</summary>
	/// <param name="scheme">Request scheme (http / https).</param>
	/// <param name="host">Request host header (host[:port]).</param>
	/// <returns>The canonical MCP URL.</returns>
	public static string InferResourceUrl(string scheme, string host) => $"{scheme}://{host}{mcpResourcePath}";
}
