using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.AI;
using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Shared.AI;

/// <summary>
/// JSON conventions for structured LLM output: one set of options drives both
/// the schema sent to the model and the deserialization of its reply, so the
/// two can't drift apart.
/// </summary>
public static class LlmJson
{
	/// <summary>
	/// Gets camelCase options with case-insensitive reads. Numbers stay strict so
	/// the generated schema advertises plain numbers rather than "number or string".
	/// </summary>
	public static JsonSerializerOptions SerializerOptions { get; } = CreateSerializerOptions();

	/// <summary>Parse a model reply into <typeparamref name="T"/>, tolerating markdown code fences.</summary>
	/// <typeparam name="T">Expected response shape.</typeparam>
	/// <param name="response">Raw reply text.</param>
	/// <returns>The parsed value, or null for a JSON <c>null</c> literal.</returns>
	/// <exception cref="JsonException">The reply is not valid JSON for <typeparamref name="T"/>.</exception>
	public static T? Deserialize<T>(string response) =>
		JsonSerializer.Deserialize<T>(LlmResponseParser.ExtractJson(response), SerializerOptions);

	/// <summary>Request options asking the model for JSON matching <typeparamref name="T"/>'s schema.</summary>
	/// <typeparam name="T">Expected response shape.</typeparam>
	/// <returns>Fresh options (callers may mutate them) carrying the cached response format.</returns>
	public static ChatOptions OptionsFor<T>() => new() { ResponseFormat = SchemaFormat<T>.Value };

	private static JsonSerializerOptions CreateSerializerOptions()
	{
		var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
		{
			NumberHandling = JsonNumberHandling.Strict,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
			TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
		};
		options.MakeReadOnly();
		return options;
	}

	// Schema generation reflects over T once per type instead of per request.
	private static class SchemaFormat<T>
	{
		public static readonly ChatResponseFormatJson Value =
			ChatResponseFormat.ForJsonSchema<T>(SerializerOptions, typeof(T).Name);
	}
}
