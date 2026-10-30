using System.Text.Json;
using System.Text.Json.Nodes;
using SmartSolutionsLab.Yumney.Shared.Capabilities;

namespace SmartSolutionsLab.Yumney.Mcp.Server.OpenApi;

/// <summary>
/// Read-only view over a capability's flat input schema
/// (<see cref="CapabilityDescriptor.InputSchema"/>) for splitting it into
/// OpenAPI path / query parameters and a request body.
/// </summary>
internal sealed class ArgumentSchema
{
	private readonly JsonObject properties;
	private readonly HashSet<string> required;

	private ArgumentSchema(JsonObject properties, HashSet<string> required)
	{
		this.properties = properties;
		this.required = required;
	}

	public static ArgumentSchema? From(JsonElement? inputSchema)
	{
		if (inputSchema is not { } schemaElement) return null;
		if (JsonNode.Parse(schemaElement.GetRawText()) is not JsonObject schema) return null;

		var properties = schema["properties"] as JsonObject ?? new JsonObject();
		var requiredNames = (schema["required"] as JsonArray ?? [])
			.Select(name => name?.GetValue<string>())
			.OfType<string>();
		return new ArgumentSchema(properties, new HashSet<string>(requiredNames, StringComparer.Ordinal));
	}

	public JsonNode? SchemaOf(string name) => properties[name]?.DeepClone();

	public bool IsRequired(string name) => required.Contains(name);

	public IReadOnlyList<string> NamesExcept(IReadOnlyList<string> excluded) =>
		[.. properties.Select(property => property.Key).Where(name => !excluded.Contains(name, StringComparer.OrdinalIgnoreCase))];

	public JsonObject ObjectSchemaFor(IReadOnlyList<string> names)
	{
		var subset = new JsonObject();
		foreach (var name in names)
		{
			subset[name] = SchemaOf(name);
		}

		var schema = new JsonObject { ["type"] = "object", ["properties"] = subset };
		var requiredSubset = names.Where(IsRequired).Select(name => (JsonNode?)name).ToArray();
		if (requiredSubset.Length > 0) schema["required"] = new JsonArray(requiredSubset);

		return schema;
	}
}
