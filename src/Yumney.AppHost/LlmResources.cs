using Aspire.Hosting.ApplicationModel;

namespace SmartSolutionsLab.Yumney.AppHost;

/// <summary>
/// Shared LLM resources for the AppHost graph. Built once in <c>Program.cs</c>
/// (parameter / ollama registration must not happen twice) and passed into
/// each <see cref="AppHostExtensions.WithLlmProvider"/> call.
/// </summary>
internal sealed record LlmResources(
	IResourceBuilder<ParameterResource>? OpenAiApiKey,
	IResourceBuilder<IOllamaResource>? Ollama);
