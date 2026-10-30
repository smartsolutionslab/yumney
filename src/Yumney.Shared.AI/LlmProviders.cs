namespace SmartSolutionsLab.Yumney.Shared.AI;

/// <summary>Provider names the AppHost passes via <see cref="ChatClientHostBuilderExtensions.ProviderKey"/>.</summary>
public static class LlmProviders
{
	/// <summary>OpenAI (or any OpenAI-compatible endpoint).</summary>
	public const string OpenAI = "OpenAI";

	/// <summary>Local Ollama server.</summary>
	public const string Ollama = "Ollama";

	/// <summary>Anthropic Claude via the official Anthropic SDK.</summary>
	public const string Anthropic = "Anthropic";
}
