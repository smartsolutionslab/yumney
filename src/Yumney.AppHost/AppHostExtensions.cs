using Aspire.Hosting.ApplicationModel;
using Yumney.AppHost;

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
    /// </summary>
    public static IResourceBuilder<ProjectResource> WithLlmProvider(
        this IResourceBuilder<ProjectResource> api,
        IDistributedApplicationBuilder builder,
        AppHostOptions options)
    {
        if (options.UseOllama)
        {
            var ollama = builder.AddOllama("ollama").WithDataVolume();
            api.WithReference(ollama).WaitFor(ollama);
        }
        else
        {
            var apiKey = builder.AddParameter("OpenAiApiKey", secret: true);
            api
                .WithEnvironment("SemanticKernel__Provider", "OpenAI")
                .WithEnvironment("SemanticKernel__ModelId", options.OpenAiModelId)
                .WithEnvironment("SemanticKernel__ApiKey", apiKey);
        }

        return api;
    }
}
