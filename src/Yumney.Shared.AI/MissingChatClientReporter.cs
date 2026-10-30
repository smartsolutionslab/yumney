using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SmartSolutionsLab.Yumney.Shared.AI;

/// <summary>
/// Logs once at startup when the host was given no chat model. Without it the
/// misconfiguration only shows up as a warning per failed LLM call, while
/// features quietly degrade (e.g. every shopping item categorized as "Other").
/// </summary>
/// <param name="logger">Logger for the startup report.</param>
internal sealed partial class MissingChatClientReporter(ILogger<MissingChatClientReporter> logger) : IHostedService
{
	public Task StartAsync(CancellationToken cancellationToken)
	{
		LogMissingChatClient(ChatClientHostBuilderExtensions.ConnectionName);
		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

	[LoggerMessage(
		Level = LogLevel.Error,
		Message = "No LLM configured: connection string '{ConnectionName}' is missing. LLM-backed features will fail or degrade.")]
	private partial void LogMissingChatClient(string connectionName);
}
