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
	// AddParameter / AddOpenAI / AddOllama throw on duplicate names, so the
	// resource has to be created in exactly one place. Either way the hosts
	// receive a model resource named "chat" (the connection string
	// Yumney.Shared.AI reads) plus the provider name.
	public static LlmResources BuildLlmResources(this IDistributedApplicationBuilder builder, AppHostOptions options)
	{
		if (options.E2ETests) return LlmResources.None;

		return options.LlmProvider switch
		{
			LlmProvider.Ollama => builder.BuildOllamaResources(options),
			LlmProvider.Anthropic => builder.BuildAnthropicResources(options),
			_ => builder.BuildOpenAiResources(options),
		};
	}

	public static IResourceBuilder<ProjectResource> WithLlmProvider(this IResourceBuilder<ProjectResource> builder, LlmResources llm)
	{
		if (llm.ChatModel is not { } chatModel) return builder;

		builder
			.WithReference(chatModel)
			.WithEnvironment("Llm__Provider", llm.Provider.ToString());

		foreach (var (task, model) in llm.TaskModels)
		{
			builder.WithEnvironment($"Llm__Models__{task}", model);
		}

		// Ollama pulls the model on first start; OpenAI models are remote and
		// never reach a "running" state, so waiting on them would block startup.
		if (llm.WaitForModel)
		{
			builder.WaitFor(chatModel);
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

	private static LlmResources BuildOllamaResources(this IDistributedApplicationBuilder builder, AppHostOptions options)
	{
		var ollamaModel = builder.AddOllama("ollama")
			.WithDataVolume()
			.AddModel(LlmResources.ChatModelName, options.OllamaModelId);
		return new LlmResources(ollamaModel, options.LlmProvider, WaitForModel: true, TaskModels: new Dictionary<string, string>());
	}

	private static LlmResources BuildOpenAiResources(this IDistributedApplicationBuilder builder, AppHostOptions options)
	{
		// Parameter name is kept stable: deploy.yml supplies it as Parameters__OpenAiApiKey.
		var apiKey = builder.AddParameter("OpenAiApiKey", secret: true);
		var openAiModel = builder.AddOpenAI("openai")
			.WithApiKey(apiKey)
			.AddModel(LlmResources.ChatModelName, options.OpenAiModelId);
		return new LlmResources(openAiModel, options.LlmProvider, WaitForModel: false, options.OpenAiTaskModels);
	}

	// No Aspire hosting integration exists for Anthropic, so the "chat" connection
	// string is composed directly; Yumney.Shared.AI reads Key and Model from it.
	// Deployments switching to Anthropic supply Parameters__AnthropicApiKey.
	private static LlmResources BuildAnthropicResources(this IDistributedApplicationBuilder builder, AppHostOptions options)
	{
		var apiKey = builder.AddParameter("AnthropicApiKey", secret: true);
		var model = options.AnthropicModelId;
		var chatModel = builder.AddConnectionString(
			LlmResources.ChatModelName,
			ReferenceExpression.Create($"Key={apiKey.Resource};Model={model}"));
		return new LlmResources(chatModel, options.LlmProvider, WaitForModel: false, options.AnthropicTaskModels);
	}
}
