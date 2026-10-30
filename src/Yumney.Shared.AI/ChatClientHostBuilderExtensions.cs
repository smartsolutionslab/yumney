using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SmartSolutionsLab.Yumney.Shared.AI;

/// <summary>
/// Single registration point for the LLM <see cref="IChatClient"/> every
/// module host uses. The AppHost hands each LLM-consuming host a model
/// resource under the <see cref="ConnectionName"/> connection string plus the
/// provider name; this maps both onto the matching Aspire client integration,
/// which also wires OpenTelemetry for the chat calls.
/// </summary>
public static class ChatClientHostBuilderExtensions
{
	/// <summary>Connection string name of the chat model resource.</summary>
	public const string ConnectionName = "chat";

	/// <summary>Configuration key carrying the provider (<c>OpenAI</c> or <c>Ollama</c>).</summary>
	public const string ProviderKey = "Llm:Provider";

	/// <summary>Configuration section mapping task roles (<see cref="LlmTasks"/>) to model names.</summary>
	public const string ModelsSection = "Llm:Models";

	/// <summary>
	/// Register the host's default <see cref="IChatClient"/> plus one keyed client per
	/// task role in <see cref="LlmTasks"/>. A role without its own model (or any role
	/// on Ollama, which serves a single local model) resolves to the default client.
	/// </summary>
	/// <param name="builder">The module host builder.</param>
	/// <returns>The same builder, for fluent chaining.</returns>
	public static IHostApplicationBuilder AddYumneyChatClient(this IHostApplicationBuilder builder)
	{
		var configuration = builder.Configuration;
		if (configuration.GetValue<bool>("E2ETests")) return builder;
		if (builder.Services.Any(descriptor => descriptor.ServiceType == typeof(IChatClient) && !descriptor.IsKeyedService)) return builder;

		IReadOnlyCollection<string> dedicatedTasks = [];
		if (string.IsNullOrWhiteSpace(configuration.GetConnectionString(ConnectionName)))
		{
			builder.Services.AddHostedService<MissingChatClientReporter>();
			builder.Services.AddSingleton<IChatClient, UnavailableChatClient>();
		}
		else if (string.Equals(configuration.GetValue<string>(ProviderKey), LlmProviders.Ollama, StringComparison.OrdinalIgnoreCase))
		{
			builder.AddOllamaApiClient(ConnectionName).AddChatClient();
		}
		else
		{
			dedicatedTasks = AddOpenAIChatClients(builder);
		}

		AliasRemainingTasksToDefault(builder.Services, dedicatedTasks);
		return builder;
	}

	private static List<string> AddOpenAIChatClients(IHostApplicationBuilder builder)
	{
		var openAi = builder.AddOpenAIClient(ConnectionName);
		openAi.AddChatClient();

		List<string> dedicatedTasks = [];
		foreach (var task in LlmTasks.All)
		{
			var model = builder.Configuration.GetValue<string>($"{ModelsSection}:{task}");
			if (string.IsNullOrWhiteSpace(model)) continue;

			openAi.AddKeyedChatClient(task, model);
			dedicatedTasks.Add(task);
		}

		return dedicatedTasks;
	}

	private static void AliasRemainingTasksToDefault(IServiceCollection services, IReadOnlyCollection<string> dedicatedTasks)
	{
		foreach (var task in LlmTasks.All.Except(dedicatedTasks))
		{
			services.AddKeyedSingleton<IChatClient>(task, (serviceProvider, _) => serviceProvider.GetRequiredService<IChatClient>());
		}
	}
}
