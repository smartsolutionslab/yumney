using Microsoft.Extensions.Configuration;

namespace SmartSolutionsLab.Yumney.AppHost.Options;

internal sealed record AppHostOptions(
    LlmProvider LlmProvider,
    string OpenAiModelId,
    string? RegistryEndpoint,
    string? RegistryRepository,
    string? GhcrUser,
    string? GhcrToken)
{
    public bool UseOllama => LlmProvider == LlmProvider.Ollama;

    public bool UseGhcr => !string.IsNullOrWhiteSpace(RegistryEndpoint);

    public bool UseGhcrPullCredentials => !string.IsNullOrWhiteSpace(GhcrUser);

    public static AppHostOptions FromConfiguration(IConfiguration config)
    {
        var providerString = config.GetValue<string>("LlmProvider") ?? "Ollama";
        var provider = Enum.TryParse<LlmProvider>(providerString, ignoreCase: true, out var parsed)
            ? parsed
            : LlmProvider.Ollama;

        return new AppHostOptions(
            LlmProvider: provider,
            OpenAiModelId: config.GetValue<string>("OpenAi:ModelId") ?? "gpt-5.4-mini",
            RegistryEndpoint: config.GetValue<string>("RegistryEndpoint"),
            RegistryRepository: config.GetValue<string>("RegistryRepository"),
            GhcrUser: config.GetValue<string>("GhcrUser"),
            GhcrToken: config.GetValue<string>("GhcrToken"));
    }
}
