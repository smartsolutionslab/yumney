using System.Data.Common;
using Anthropic;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SmartSolutionsLab.Yumney.Shared.AI;

/// <summary>
/// Registers Claude chat clients through the official Anthropic SDK's
/// <c>AsIChatClient</c> adapter. There is no Aspire client integration for
/// Anthropic, so this reads the same <c>chat</c> connection string
/// (<c>Key=…;Model=…</c>) the AppHost builds and adds the OpenTelemetry and
/// logging middleware the Aspire integrations add for the other providers.
/// </summary>
internal static class AnthropicChatClients
{
	// Non-streaming requests; keeps responses well under SDK HTTP timeouts
	// while leaving room for full recipe extractions and week plans.
	private const int DefaultMaxOutputTokens = 16_000;

	public static List<string> Add(IHostApplicationBuilder builder)
	{
		var (apiKey, model) = ReadConnection(builder.Configuration);
		builder.Services.AddSingleton<IAnthropicClient>(_ => new AnthropicClient { ApiKey = apiKey });

		builder.Services.AddChatClient(serviceProvider => CreateClient(serviceProvider, model))
			.UseOpenTelemetry()
			.UseLogging();

		List<string> dedicatedTasks = [];
		foreach (var task in LlmTasks.All)
		{
			var taskModel = builder.Configuration.GetValue<string>($"{ChatClientHostBuilderExtensions.ModelsSection}:{task}");
			if (string.IsNullOrWhiteSpace(taskModel)) continue;

			builder.Services.AddKeyedChatClient(task, serviceProvider => CreateClient(serviceProvider, taskModel))
				.UseOpenTelemetry()
				.UseLogging();
			dedicatedTasks.Add(task);
		}

		return dedicatedTasks;
	}

	private static IChatClient CreateClient(IServiceProvider serviceProvider, string model) =>
		serviceProvider.GetRequiredService<IAnthropicClient>().AsIChatClient(model, DefaultMaxOutputTokens);

	private static (string ApiKey, string Model) ReadConnection(IConfiguration configuration)
	{
		var connection = new DbConnectionStringBuilder
		{
			ConnectionString = configuration.GetConnectionString(ChatClientHostBuilderExtensions.ConnectionName),
		};
		var apiKey = connection.TryGetValue("Key", out var key) ? key?.ToString() : null;
		var model = connection.TryGetValue("Model", out var modelName) ? modelName?.ToString() : null;
		if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(model))
		{
			throw new InvalidOperationException("The 'chat' connection string for Anthropic needs both 'Key' and 'Model'.");
		}

		return (apiKey, model);
	}
}
