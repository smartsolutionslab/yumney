namespace SmartSolutionsLab.Yumney.Mcp.Server.Auth;

/// <summary>
/// Builds the RFC 6750 <c>WWW-Authenticate</c> header value that points MCP
/// clients at the RFC 9728 discovery document. Pure functions only — wiring
/// into <c>JwtBearerEvents.OnChallenge</c> lives in
/// <see cref="KeycloakAuthExtensions"/> so this stays unit-testable.
/// </summary>
public static class WwwAuthenticateChallenge
{
#pragma warning disable SA1303
	private const string discoveryPath = "/.well-known/oauth-protected-resource";
#pragma warning restore SA1303

	/// <summary>
	/// Composes the header value. RFC 6750 §3 says <c>error</c> MUST be omitted
	/// when the client sent no credentials at all — pass <c>null</c> in that case.
	/// </summary>
	/// <param name="realm">Protection-space identifier (e.g. <c>yumney</c>).</param>
	/// <param name="discoveryUrl">Absolute URL of the RFC 9728 metadata document.</param>
	/// <param name="error">Optional RFC 6750 error code, e.g. <c>invalid_token</c>.</param>
	/// <returns>The full header value, prefixed with <c>Bearer</c>.</returns>
	public static string BuildHeader(string realm, string discoveryUrl, string? error)
	{
		var parameters = new List<string>(capacity: 3)
		{
			$"realm=\"{realm}\"",
			$"resource_metadata=\"{discoveryUrl}\"",
		};

		if (!string.IsNullOrEmpty(error))
		{
			parameters.Add($"error=\"{error}\"");
		}

		return $"Bearer {string.Join(", ", parameters)}";
	}

	/// <summary>
	/// Resolves the absolute discovery URL the header should advertise. When
	/// <paramref name="configuredResourceUrl"/> is set, the host portion is
	/// reused and <c>/mcp</c> is replaced with the discovery path — clients
	/// behind a gateway need the public origin, not the internal container's.
	/// </summary>
	/// <param name="request">Inbound request — used as the fallback origin.</param>
	/// <param name="configuredResourceUrl">Public MCP URL from <c>McpServer:PublicUrl</c>, or null.</param>
	/// <returns>Absolute discovery URL.</returns>
	public static string ResolveDiscoveryUrl(HttpRequest request, string? configuredResourceUrl)
	{
		if (!string.IsNullOrWhiteSpace(configuredResourceUrl))
		{
			var origin = ExtractOrigin(configuredResourceUrl);
			return $"{origin}{discoveryPath}";
		}

		return $"{request.Scheme}://{request.Host}{discoveryPath}";
	}

	private static string ExtractOrigin(string absoluteUrl)
	{
		var uri = new Uri(absoluteUrl, UriKind.Absolute);
		return $"{uri.Scheme}://{uri.Authority}";
	}
}
