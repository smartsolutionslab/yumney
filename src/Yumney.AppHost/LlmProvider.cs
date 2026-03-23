namespace SmartSolutionsLab.Yumney.AppHost;

/// <summary>
/// LLM provider for recipe extraction.
/// </summary>
public enum LlmProvider
{
    /// <summary>Local Ollama instance.</summary>
    Ollama,

    /// <summary>OpenAI API.</summary>
    OpenAI,
}
