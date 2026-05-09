using SmartSolutionsLab.Yumney.AppHost.Options;

namespace SmartSolutionsLab.Yumney.AppHost;

internal static class AppHostExtensions
{
	public static IResourceBuilder<ProjectResource> AsYumneyApi(
		this IResourceBuilder<ProjectResource> api,
		AppHostOptions options,
		IResourceBuilder<IResourceWithConnectionString> database,
		IResourceBuilder<KeycloakResource> keycloak,
		IResourceBuilder<IResourceWithConnectionString> redis,
		IResourceBuilder<IResourceWithConnectionString> messaging,
		IResourceBuilder<ProjectResource> migrationRunner)
	{
		var configured = api
			.WithHttpEndpoint()
			.WithReference(database)
			.WaitFor(migrationRunner)
			.WithReference(keycloak)
			.WithReference(redis)
			.WithReference(messaging)
			.WithUrl("/scalar/v1", "Scalar");

		if (options.E2ETests) configured.WithEnvironment("E2ETests", "true");

		return configured;
	}

	public static IResourceBuilder<ProjectResource> WithLlmProvider(
		this IResourceBuilder<ProjectResource> builder,
		IDistributedApplicationBuilder aspireBuilder,
		AppHostOptions options)
	{
		if (options.E2ETests) return builder;

		if (options.UseOllama)
		{
			var ollama = aspireBuilder.AddOllama("ollama").WithDataVolume();
			builder.WithReference(ollama).WaitFor(ollama);
		}
		else
		{
			var apiKey = aspireBuilder.AddParameter("OpenAiApiKey", secret: true);
			builder
				.WithEnvironment("SemanticKernel__Provider", "OpenAI")
				.WithEnvironment("SemanticKernel__ModelId", options.OpenAiModelId)
				.WithEnvironment("SemanticKernel__ApiKey", apiKey);
		}

		return builder;
	}
}
