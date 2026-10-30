using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SmartSolutionsLab.Yumney.Shared.Web.Capabilities;

/// <summary>
/// Projects an endpoint's <see cref="ApiDescription"/> into the flat JSON
/// Schema an LLM tool needs: route + query parameters as top-level properties,
/// plus the request body's properties lifted to the same level (the MCP proxy
/// sends every non-route argument of a POST/PUT/PATCH as the JSON body).
/// </summary>
internal static class CapabilityInputSchemaBuilder
{
	public static JsonElement Build(ApiDescription description, JsonSerializerOptions serializerOptions)
	{
		var schemaOptions = CreateSchemaOptions(serializerOptions);
		var properties = new JsonObject();
		var required = new JsonArray();

		foreach (var parameter in description.ParameterDescriptions)
		{
			if (parameter.Source == BindingSource.Body)
			{
				AddBodyProperties(parameter, schemaOptions, properties, required);
			}
			else if (parameter.Source == BindingSource.Path || parameter.Source == BindingSource.Query)
			{
				AddParameter(parameter, schemaOptions, properties, required);
			}
		}

		var schema = new JsonObject { ["type"] = "object", ["properties"] = properties };
		if (required.Count > 0) schema["required"] = required;

		return JsonSerializer.SerializeToElement(schema);
	}

	// The host's serializer stays lenient; only the schema view treats
	// non-defaulted constructor parameters and non-nullable references as
	// required, and advertises plain numbers instead of the web default's
	// "number or numeric string" — what the endpoint expects from a caller.
	private static JsonSerializerOptions CreateSchemaOptions(JsonSerializerOptions serializerOptions)
	{
		var schemaOptions = new JsonSerializerOptions(serializerOptions)
		{
			NumberHandling = JsonNumberHandling.Strict,
			RespectNullableAnnotations = true,
			RespectRequiredConstructorParameters = true,
			TypeInfoResolver = serializerOptions.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver(),
		};
		schemaOptions.MakeReadOnly();
		return schemaOptions;
	}

	private static void AddParameter(
		ApiParameterDescription parameter,
		JsonSerializerOptions schemaOptions,
		JsonObject properties,
		JsonArray required)
	{
		properties[parameter.Name] = parameter.Type is null
			? new JsonObject { ["type"] = "string" }
			: schemaOptions.GetJsonSchemaAsNode(parameter.Type);

		var isRequired = parameter.Source == BindingSource.Path || parameter.IsRequired;
		if (isRequired) required.Add(parameter.Name);
	}

	private static void AddBodyProperties(
		ApiParameterDescription parameter,
		JsonSerializerOptions schemaOptions,
		JsonObject properties,
		JsonArray required)
	{
		if (parameter.Type is null) return;

		var bodySchema = schemaOptions.GetJsonSchemaAsNode(parameter.Type);
		if (bodySchema["properties"] is not JsonObject bodyProperties) return;

		foreach (var (name, propertySchema) in bodyProperties)
		{
			properties[name] = propertySchema?.DeepClone();
		}

		if (bodySchema["required"] is not JsonArray bodyRequired) return;

		foreach (var name in bodyRequired)
		{
			required.Add(name?.DeepClone());
		}
	}
}
