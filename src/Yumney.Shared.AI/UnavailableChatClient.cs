using Microsoft.Extensions.AI;

namespace SmartSolutionsLab.Yumney.Shared.AI;

/// <summary>
/// Stand-in registered when a host has no chat model. Services can still be
/// constructed (keeping e.g. shopping-list creation alive), and each LLM call
/// fails inside the service's own error handling instead of at DI resolution.
/// </summary>
internal sealed class UnavailableChatClient : IChatClient
{
	private const string Message = "No LLM is configured for this host (missing 'chat' connection string).";

	public Task<ChatResponse> GetResponseAsync(
		IEnumerable<ChatMessage> messages,
		ChatOptions? options = null,
		CancellationToken cancellationToken = default) =>
		throw new InvalidOperationException(Message);

	public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
		IEnumerable<ChatMessage> messages,
		ChatOptions? options = null,
		CancellationToken cancellationToken = default) =>
		throw new InvalidOperationException(Message);

	public object? GetService(Type serviceType, object? serviceKey = null) =>
		serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

	public void Dispose()
	{
	}
}
