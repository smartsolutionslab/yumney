namespace SmartSolutionsLab.Yumney.AppHost.Options;

/// <summary>Supported LLM providers for recipe extraction.</summary>
internal enum LlmProvider
{
    /// <summary>Local Ollama instance.</summary>
    Ollama,

    /// <summary>OpenAI API.</summary>
    OpenAI,
}
