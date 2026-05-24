using System.Globalization;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning.AppContainers;
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
			.WithUrls(AddScalarAndOpenApiUrls);

		if (options.E2ETests) configured.WithEnvironment("E2ETests", "true");

		return configured;
	}

	// Call once from Program.cs before any WithLlmProvider usage —
	// AddParameter / AddOllama throw on duplicate names, so the
	// resource has to be created in exactly one place.
	public static LlmResources BuildLlmResources(this IDistributedApplicationBuilder builder, AppHostOptions options)
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

	public static IResourceBuilder<ProjectResource> PublishAsScaledContainerApp(
		this IResourceBuilder<ProjectResource> project,
		int min,
		int max,
		int? concurrentRequests = null) =>
		project.PublishAsAzureContainerApp(ConfigureScaling(min, max, concurrentRequests));

	public static IResourceBuilder<ContainerResource> PublishAsScaledContainerApp(
		this IResourceBuilder<ContainerResource> container,
		int min,
		int max,
		int? concurrentRequests = null) =>
		container.PublishAsAzureContainerApp(ConfigureScaling(min, max, concurrentRequests));

	private static Action<AzureResourceInfrastructure, ContainerApp> ConfigureScaling(int min, int max, int? concurrentRequests) =>
		(_, app) =>
		{
			app.Template.Scale.MinReplicas = min;
			app.Template.Scale.MaxReplicas = max;

			if (concurrentRequests.HasValue)
			{
				app.Template.Scale.Rules.Add(new ContainerAppScaleRule
				{
					Name = "http-scaling",
					Http = new ContainerAppHttpScaleRule
					{
						Metadata =
						{
							{
								"concurrentRequests",
								concurrentRequests.Value.ToString(CultureInfo.InvariantCulture)
							},
						},
					},
				});
			}
		};

	// The plain `.WithUrl("/scalar/v1", "Scalar")` overload takes a literal
	// URL — Aspire has no endpoint context to resolve a leading slash, so
	// the dashboard silently drops the entry. Per Aspire docs (define-
	// custom-resource-urls), additive URLs that need to be joined to a
	// runtime endpoint go through `WithUrls(callback)`: walk the existing
	// URL set for the http endpoint, then append deep links on it.
	private static void AddScalarAndOpenApiUrls(ResourceUrlsCallbackContext context)
	{
		var http = context.Urls.FirstOrDefault(url => url.Endpoint?.EndpointName == "http");
		if (http is null) return;

		context.Urls.Add(new ResourceUrlAnnotation
		{
			Url = $"{http.Url.TrimEnd('/')}/scalar/v1",
			Endpoint = http.Endpoint,
			DisplayText = "Scalar",
		});
		context.Urls.Add(new ResourceUrlAnnotation
		{
			Url = $"{http.Url.TrimEnd('/')}/openapi/v1.json",
			Endpoint = http.Endpoint,
			DisplayText = "OpenAPI",
		});
	}
}
