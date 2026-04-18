namespace SmartSolutionsLab.Yumney.Recipes.Extraction.Services;

public sealed class SemanticKernelOptions
{
	public const string SectionName = "SemanticKernel";

	public const string ProviderOpenAI = "OpenAI";

	public const string ProviderAzureOpenAI = "AzureOpenAI";

	public const string ProviderOllama = "Ollama";

	public string Provider { get; init; } = ProviderOpenAI;

	public string ModelId { get; init; } = string.Empty;

	public string ApiKey { get; init; } = string.Empty;

	public string Endpoint { get; init; } = string.Empty;

	public void Deconstruct(out string provider, out string modelId, out string endpoint, out string apiKey)
	{
		provider = this.Provider;
		modelId = this.ModelId;
		endpoint = this.Endpoint;
		apiKey = this.ApiKey;
	}
}
