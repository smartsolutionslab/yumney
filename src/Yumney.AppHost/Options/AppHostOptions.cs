using Microsoft.Extensions.Configuration;

namespace SmartSolutionsLab.Yumney.AppHost.Options;

internal sealed record AppHostOptions(
	bool DatabaseOnly,
	bool E2ETests,
	LlmProvider LlmProvider,
	string OpenAiModelId,
	string OllamaModelId,
	IReadOnlyDictionary<string, string> OpenAiTaskModels,
	string? OpenAiEndpoint,
	string? OpenAiStructuredOutput)
{
	public bool UseOllama => LlmProvider == LlmProvider.Ollama;

	public static AppHostOptions FromConfiguration(IConfiguration config)
	{
		var providerString = config.GetValue<string>("LlmProvider") ?? "Ollama";
		var provider = Enum.TryParse<LlmProvider>(providerString, ignoreCase: true, out var parsed)
			? parsed
			: LlmProvider.Ollama;

		return new AppHostOptions(
			DatabaseOnly: config.GetValue<bool>("DatabaseOnly"),
			E2ETests: config.GetValue<bool>("E2ETests"),
			LlmProvider: provider,
			OpenAiModelId: config.GetValue<string>("OpenAi:ModelId") ?? "gpt-5.4-mini",
			OllamaModelId: config.GetValue<string>("Ollama:ModelId") ?? "mistral",
			OpenAiTaskModels: ReadTaskModels(config.GetSection("OpenAi:Models")),
			OpenAiEndpoint: config.GetValue<string>("OpenAi:Endpoint"),
			OpenAiStructuredOutput: config.GetValue<string>("OpenAi:StructuredOutput"));
	}

	// Optional per-task model overrides (e.g. OpenAi:Models:fast, OpenAi:Models:vision);
	// task roles without an entry use OpenAiModelId.
	private static Dictionary<string, string> ReadTaskModels(IConfigurationSection section)
	{
		return section.GetChildren()
			.Where(child => !string.IsNullOrWhiteSpace(child.Value))
			.ToDictionary(child => child.Key.ToLowerInvariant(), child => child.Value!);
	}
}
