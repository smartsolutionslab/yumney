using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SmartSolutionsLab.Yumney.Mcp.Server.Mcp;

/// <summary>
/// Substitutes <c>{name:type}</c> route placeholders with values pulled from
/// the MCP tool argument bag, and assembles the remaining arguments into a
/// query string (for GET) or returns them as the JSON body (for POST/PUT).
/// </summary>
public static partial class RouteUrlBuilder
{
	/// <summary>The result of building a request from a route pattern + tool arguments.</summary>
	/// <param name="Url">Final URL with placeholders substituted and query string appended where applicable.</param>
	/// <param name="Body">JSON body for POST/PUT, or null for GET/DELETE.</param>
	/// <param name="MissingPlaceholders">Placeholder names that didn't have a matching argument — caller should fail the call.</param>
	public sealed record BuiltRequest(string Url, string? Body, IReadOnlyList<string> MissingPlaceholders);

	/// <summary>Build a request URL + body for an HTTP call to a module endpoint.</summary>
	/// <param name="httpMethod">HTTP method (case-insensitive).</param>
	/// <param name="routePattern">Route pattern from a <c>CapabilityDescriptor</c>.</param>
	/// <param name="arguments">Tool arguments by name. Supports null arguments dictionary.</param>
	/// <returns>Built request — caller must check <c>MissingPlaceholders</c> before sending.</returns>
	public static BuiltRequest Build(
		string httpMethod,
		string routePattern,
		IDictionary<string, JsonElement>? arguments)
	{
		arguments ??= new Dictionary<string, JsonElement>();
		var consumedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var missing = new List<string>();

		var path = PlaceholderPattern().Replace(routePattern, match =>
		{
			var placeholderName = match.Groups["name"].Value;
			if (!arguments.TryGetValue(placeholderName, out var value))
			{
				missing.Add(placeholderName);
				return match.Value;
			}

			consumedKeys.Add(placeholderName);
			return Uri.EscapeDataString(JsonElementToString(value));
		});

		var unconsumed = arguments
			.Where(pair => !consumedKeys.Contains(pair.Key))
			.ToList();

		if (UsesBody(httpMethod))
		{
			var body = unconsumed.Count == 0
				? null
				: JsonSerializer.Serialize(unconsumed.ToDictionary(pair => pair.Key, pair => pair.Value));
			return new BuiltRequest(path, body, missing);
		}

		var url = unconsumed.Count == 0 ? path : $"{path}?{BuildQueryString(unconsumed)}";
		return new BuiltRequest(url, Body: null, missing);
	}

	private static bool UsesBody(string httpMethod) =>
		HttpMethod.Post.Method.Equals(httpMethod, StringComparison.OrdinalIgnoreCase)
		|| HttpMethod.Put.Method.Equals(httpMethod, StringComparison.OrdinalIgnoreCase)
		|| HttpMethod.Patch.Method.Equals(httpMethod, StringComparison.OrdinalIgnoreCase);

	private static string BuildQueryString(IReadOnlyList<KeyValuePair<string, JsonElement>> pairs)
	{
		var query = new StringBuilder();
		var first = true;
		foreach (var (key, value) in pairs)
		{
			if (!first) query.Append('&');
			query.Append(Uri.EscapeDataString(key));
			query.Append('=');
			query.Append(Uri.EscapeDataString(JsonElementToString(value)));
			first = false;
		}

		return query.ToString();
	}

	private static string JsonElementToString(JsonElement value) => value.ValueKind switch
	{
		JsonValueKind.String => value.GetString() ?? string.Empty,
		JsonValueKind.Number => value.GetRawText(),
		JsonValueKind.True => "true",
		JsonValueKind.False => "false",
		JsonValueKind.Null => string.Empty,
		_ => value.GetRawText(),
	};

	[GeneratedRegex(@"\{(?<name>[a-zA-Z_][a-zA-Z0-9_]*)(?::[^}]+)?\}")]
	private static partial Regex PlaceholderPattern();
}
