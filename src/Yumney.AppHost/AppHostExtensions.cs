using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting;

internal static class AppHostExtensions
{
    /// <summary>
    /// Configures standard Yumney API dependencies: HTTP endpoint, Keycloak auth,
    /// database, Redis cache, RabbitMQ messaging, migration runner wait, and Scalar docs.
    /// </summary>
    public static IResourceBuilder<ProjectResource> AsYumneyApi(
        this IResourceBuilder<ProjectResource> api,
        IResourceBuilder<IResourceWithConnectionString> database,
        IResourceBuilder<KeycloakResource> keycloak,
        IResourceBuilder<IResourceWithConnectionString> redis,
        IResourceBuilder<IResourceWithConnectionString> messaging,
        IResourceBuilder<ProjectResource> migrationRunner)
    {
        return api
            .WithHttpEndpoint()
            .WithReference(keycloak)
            .WithReference(database)
            .WithReference(redis)
            .WithReference(messaging)
            .WaitFor(keycloak)
            .WaitFor(migrationRunner)
            .WaitFor(redis)
            .WaitFor(messaging)
            .WithUrlForEndpoint("http", url =>
            {
                url.DisplayText = "Scalar";
                url.Url = "/scalar/v1";
            });
    }

    /// <summary>
    /// Configures the LLM provider — Ollama (local dev) or OpenAI (staging/prod).
    /// Reads LlmProvider, OpenAi:ModelId, and OpenAiApiKey from configuration.
    /// </summary>
    public static IResourceBuilder<ProjectResource> WithLlmProvider(
        this IResourceBuilder<ProjectResource> api,
        IDistributedApplicationBuilder builder,
        IConfiguration config)
    {
        var provider = config.GetValue<string>("LlmProvider") ?? "Ollama";

        if (provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
        {
            var ollama = builder.AddOllama("ollama").WithDataVolume();
            api.WithReference(ollama).WaitFor(ollama);
        }
        else
        {
            var apiKey = builder.AddParameter("OpenAiApiKey", secret: true);
            api
                .WithEnvironment("SemanticKernel__Provider", "OpenAI")
                .WithEnvironment("SemanticKernel__ModelId", config.GetValue<string>("OpenAi:ModelId") ?? "gpt-5.4-mini")
                .WithEnvironment("SemanticKernel__ApiKey", apiKey);
        }

        return api;
    }
}
