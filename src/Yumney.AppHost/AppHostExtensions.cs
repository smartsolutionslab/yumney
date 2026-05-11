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

	/// <summary>
	/// Registers the LLM provider's shared resources on the AppHost. Call once
	/// from <c>Program.cs</c> before any <see cref="WithLlmProvider"/> usage —
	/// <c>AddParameter</c> / <c>AddOllama</c> throw on duplicate names, so the
	/// resource has to be created in exactly one place.
	/// </summary>
	/// <param name="builder">The AppHost's distributed application builder.</param>
	/// <param name="options">App host options that decide between OpenAI and Ollama.</param>
	/// <returns>A handle to the registered resources; empty when E2E tests are on.</returns>
	public static LlmResources BuildLlmResources(
		this IDistributedApplicationBuilder builder,
		AppHostOptions options)
	{
		if (options.E2ETests) return new LlmResources(null, null);

		if (options.UseOllama)
		{
			var ollama = builder.AddOllama("ollama").WithDataVolume();
			return new LlmResources(null, ollama);
		}

		var apiKey = builder.AddParameter("OpenAiApiKey", secret: true);
		return new LlmResources(apiKey, null);
	}

	public static IResourceBuilder<ProjectResource> WithLlmProvider(
		this IResourceBuilder<ProjectResource> builder,
		AppHostOptions options,
		LlmResources llm)
	{
		if (options.E2ETests) return builder;

		if (llm.Ollama is { } ollama)
		{
			builder.WithReference(ollama).WaitFor(ollama);
		}
		else if (llm.OpenAiApiKey is { } apiKey)
		{
			builder
				.WithEnvironment("SemanticKernel__Provider", "OpenAI")
				.WithEnvironment("SemanticKernel__ModelId", options.OpenAiModelId)
				.WithEnvironment("SemanticKernel__ApiKey", apiKey);
		}

		return builder;
	}
}
