using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using SmartSolutionsLab.Yumney.Mcp.Server.Discovery;
using SmartSolutionsLab.Yumney.Shared.Capabilities;

namespace SmartSolutionsLab.Yumney.Mcp.Server.OpenApi;

/// <summary>
/// Builds an OpenAPI 3.1 spec covering the MCP-exposed capability surface —
/// the same operations <c>RestProxyService</c> can invoke. ChatGPT Custom GPT
/// Actions ingest this URL; the resulting GPT calls Yumney through the same
/// gateway + OAuth path as Claude.ai's MCP custom connector.
/// </summary>
/// <remarks>
/// First cut emits a minimal-but-valid spec: paths, methods, path parameters
/// extracted from route templates, OAuth2 security pointing at Keycloak, and
/// permissive request bodies on non-GET operations. Schema enrichment per
/// operation (request/response shapes) is a follow-up; this gives ChatGPT
/// enough surface to make calls, with the LLM relying on the description
/// text for argument shapes.
/// </remarks>
public static partial class OpenApiCapabilityBuilder
{
	/// <summary>Build the spec document for the discovered MCP-surface capabilities.</summary>
	/// <param name="registry">Registry populated by <see cref="CapabilityDiscoveryService"/>.</param>
	/// <param name="serverUrl">Public URL of the Yumney gateway (e.g. <c>https://yumney-gateway.example.com</c>).</param>
	/// <param name="authorizationUrl">Keycloak authorization endpoint.</param>
	/// <param name="tokenUrl">Keycloak token endpoint.</param>
	/// <returns>OpenAPI 3.1 document as a JSON node tree.</returns>
	public static JsonObject Build(AggregatedCapabilityRegistry registry, string serverUrl, string authorizationUrl, string tokenUrl)
	{
		var paths = new JsonObject();
		foreach (var capability in registry.AllCapabilities().Where(capability => capability.Surfaces.HasFlag(CapabilitySurface.Mcp)))
		{
			AddOperation(paths, capability);
		}

		return new JsonObject
		{
			["openapi"] = "3.1.0",
			["info"] = new JsonObject
			{
				["title"] = "Yumney MCP Capability Surface",
				["description"] = "OpenAPI mirror of the MCP-exposed tools — usable as a ChatGPT Custom GPT Action.",
				["version"] = "1.0.0",
			},
			["servers"] = new JsonArray { new JsonObject { ["url"] = serverUrl } },
			["paths"] = paths,
			["components"] = new JsonObject
			{
				["securitySchemes"] = new JsonObject
				{
					["keycloak"] = new JsonObject
					{
						["type"] = "oauth2",
						["flows"] = new JsonObject
						{
							["authorizationCode"] = new JsonObject
							{
								["authorizationUrl"] = authorizationUrl,
								["tokenUrl"] = tokenUrl,
								["scopes"] = new JsonObject
								{
									["openid"] = "OpenID Connect",
									["profile"] = "User profile",
									["yumney-api"] = "Yumney API access",
								},
							},
						},
					},
				},
			},
			["security"] = new JsonArray
			{
				new JsonObject { ["keycloak"] = new JsonArray { "openid", "profile", "yumney-api" } },
			},
		};
	}

	private static void AddOperation(JsonObject paths, CapabilityDescriptor capability)
	{
		var openApiPath = NormalisePath(capability.RoutePattern);
		var pathItem = paths[openApiPath] as JsonObject ?? new JsonObject();
		paths[openApiPath] = pathItem;

		var operation = new JsonObject
		{
			["operationId"] = capability.Name,
			["summary"] = capability.Name,
			["description"] = capability.Description,
			["parameters"] = BuildPathParameters(capability.RoutePattern),
			["responses"] = new JsonObject
			{
				["200"] = new JsonObject { ["description"] = "Successful response." },
				["401"] = new JsonObject { ["description"] = "Bearer token missing or invalid." },
				["403"] = new JsonObject { ["description"] = "Bearer token lacks the yumney-api scope." },
			},
		};

		if (RequiresBody(capability.HttpMethod))
		{
			operation["requestBody"] = new JsonObject
			{
				["required"] = true,
				["content"] = new JsonObject
				{
					["application/json"] = new JsonObject
					{
						["schema"] = new JsonObject
						{
							["type"] = "object",
							["additionalProperties"] = true,
							["description"] = "See the tool description for the expected fields.",
						},
					},
				},
			};
		}

		pathItem[capability.HttpMethod.ToLowerInvariant()] = operation;
	}

	private static JsonArray BuildPathParameters(string routePattern)
	{
		var parameters = new JsonArray();
		foreach (Match match in PlaceholderPattern().Matches(routePattern))
		{
			var name = match.Groups["name"].Value;
			parameters.Add(new JsonObject
			{
				["name"] = name,
				["in"] = "path",
				["required"] = true,
				["schema"] = new JsonObject { ["type"] = "string" },
			});
		}

		return parameters;
	}

	private static string NormalisePath(string routePattern) =>
		PlaceholderPattern().Replace(routePattern, "{${name}}");

	private static bool RequiresBody(string httpMethod) =>
		httpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase)
			|| httpMethod.Equals("PUT", StringComparison.OrdinalIgnoreCase)
			|| httpMethod.Equals("PATCH", StringComparison.OrdinalIgnoreCase);

	[GeneratedRegex(@"\{(?<name>[a-zA-Z_][a-zA-Z0-9_]*)(:[^}]+)?\}")]
	private static partial Regex PlaceholderPattern();
}
