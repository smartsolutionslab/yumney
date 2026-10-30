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

	/// <summary>Register the host's <see cref="IChatClient"/> from Aspire-provided configuration.</summary>
	/// <param name="builder">The module host builder.</param>
	/// <returns>The same builder, for fluent chaining.</returns>
	public static IHostApplicationBuilder AddYumneyChatClient(this IHostApplicationBuilder builder)
	{
		var configuration = builder.Configuration;
		if (configuration.GetValue<bool>("E2ETests")) return builder;
		if (builder.Services.Any(descriptor => descriptor.ServiceType == typeof(IChatClient))) return builder;

		if (string.IsNullOrWhiteSpace(configuration.GetConnectionString(ConnectionName)))
		{
			builder.Services.AddHostedService<MissingChatClientReporter>();
			return builder;
		}

		var provider = configuration.GetValue<string>(ProviderKey);
		if (string.Equals(provider, LlmProviders.Ollama, StringComparison.OrdinalIgnoreCase))
		{
			builder.AddOllamaApiClient(ConnectionName).AddChatClient();
		}
		else
		{
			builder.AddOpenAIClient(ConnectionName).AddChatClient();
		}

		return builder;
	}
}
