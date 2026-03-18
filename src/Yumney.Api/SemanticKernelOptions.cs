namespace SmartSolutionsLab.Yumney.Api;

public sealed class SemanticKernelOptions
{
    public const string SectionName = "SemanticKernel";

    public const string ProviderOpenAI = "OpenAI";

    public const string ProviderAzureOpenAI = "AzureOpenAI";

    public const string ProviderOllama = "Ollama";

    public string Provider { get; init; } = ProviderOpenAI;

    public string ModelId { get; init; } = "gpt-5.2-chat-latest";

    public string ApiKey { get; init; } = string.Empty;

    public string Endpoint { get; init; } = string.Empty;
}
