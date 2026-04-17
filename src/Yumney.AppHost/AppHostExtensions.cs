using SmartSolutionsLab.Yumney.AppHost.Options;

namespace SmartSolutionsLab.Yumney.AppHost;

internal static class AppHostExtensions
{
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
            .WithReference(database)
            .WaitFor(migrationRunner)
            .WithReference(keycloak)
            .WithReference(redis)
            .WithReference(messaging)
            .WithUrl("/scalar/v1", "Scalar");
    }

    public static IResourceBuilder<ProjectResource> WithLlmProvider(
        this IResourceBuilder<ProjectResource> api,
        IDistributedApplicationBuilder builder,
        AppHostOptions options)
    {
        if (options.E2ETests)
            return api;

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
