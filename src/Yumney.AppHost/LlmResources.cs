using SmartSolutionsLab.Yumney.AppHost.Options;

namespace SmartSolutionsLab.Yumney.AppHost;

/// <summary>
/// Shared LLM resources for the AppHost graph. Built once in <c>Program.cs</c>
/// (parameter / model registration must not happen twice) and passed into
/// each <see cref="AppHostExtensions.WithLlmProvider"/> call.
/// </summary>
/// <param name="ChatModel">Model resource exposed to hosts as the <see cref="ChatModelName"/> connection string; null in E2E mode.</param>
/// <param name="Provider">Provider the hosts select their chat client by.</param>
/// <param name="WaitForModel">Whether hosts should wait for the model resource before starting.</param>
/// <param name="TaskModels">Per-task model overrides on the same provider account (task role → model name).</param>
internal sealed record LlmResources(
	IResourceBuilder<IResourceWithConnectionString>? ChatModel,
	LlmProvider Provider,
	bool WaitForModel,
	IReadOnlyDictionary<string, string> TaskModels)
{
	public const string ChatModelName = "chat";

	public static LlmResources None { get; } = new(null, LlmProvider.OpenAI, WaitForModel: false, new Dictionary<string, string>());
}
