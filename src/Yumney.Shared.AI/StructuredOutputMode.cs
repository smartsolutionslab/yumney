namespace SmartSolutionsLab.Yumney.Shared.AI;

/// <summary>
/// How far a provider honours structured-output requests, configured per host via
/// <c>Llm:StructuredOutput</c>. Services always ask for a JSON schema; weaker
/// OpenAI-compatible providers get the request downgraded instead of a 400.
/// </summary>
public enum StructuredOutputMode
{
	/// <summary>Send the JSON schema (OpenAI, Ollama, most current providers). Default.</summary>
	JsonSchema,

	/// <summary>Send plain JSON mode (<c>json_object</c>); the prompts still describe the shape.</summary>
	JsonObject,

	/// <summary>Send no response format at all; rely on the prompts and tolerant parsing.</summary>
	None,
}
