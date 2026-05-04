using Microsoft.Extensions.Configuration;

namespace SmartSolutionsLab.Yumney.AppHost.Options;

internal sealed record AppHostOptions(
	bool DatabaseOnly,
	bool E2ETests,
	LlmProvider LlmProvider,
	string OpenAiModelId)
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
			OpenAiModelId: config.GetValue<string>("OpenAi:ModelId") ?? "gpt-5.4-mini");
	}
}
