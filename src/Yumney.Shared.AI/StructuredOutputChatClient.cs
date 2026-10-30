using Microsoft.Extensions.AI;

namespace SmartSolutionsLab.Yumney.Shared.AI;

/// <summary>
/// Rewrites JSON-schema response formats to what the provider supports
/// (<see cref="StructuredOutputMode"/>). Parsing stays the same either way:
/// replies go through <see cref="LlmJson.Deserialize{T}"/>, and every
/// structured prompt still describes the expected JSON.
/// </summary>
/// <param name="innerClient">The provider chat client.</param>
/// <param name="mode">The strongest structured-output form the provider accepts.</param>
internal sealed class StructuredOutputChatClient(IChatClient innerClient, StructuredOutputMode mode) : DelegatingChatClient(innerClient)
{
	public override Task<ChatResponse> GetResponseAsync(
		IEnumerable<ChatMessage> messages,
		ChatOptions? options = null,
		CancellationToken cancellationToken = default) =>
		base.GetResponseAsync(messages, Adapt(options), cancellationToken);

	public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
		IEnumerable<ChatMessage> messages,
		ChatOptions? options = null,
		CancellationToken cancellationToken = default) =>
		base.GetStreamingResponseAsync(messages, Adapt(options), cancellationToken);

	private ChatOptions? Adapt(ChatOptions? options)
	{
		if (options?.ResponseFormat is not ChatResponseFormatJson { Schema: not null }) return options;

		var adapted = options.Clone();
		adapted.ResponseFormat = mode == StructuredOutputMode.JsonObject ? ChatResponseFormat.Json : null;
		return adapted;
	}
}
