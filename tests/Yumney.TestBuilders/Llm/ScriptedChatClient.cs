using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace SmartSolutionsLab.Yumney.TestBuilders.Llm;

/// <summary>
/// Test double for <see cref="IChatClient"/>: replays scripted replies in
/// order (the last one repeats), or throws the configured exception, and
/// captures what the service sent so tests can assert on prompts and options.
/// </summary>
public sealed class ScriptedChatClient : IChatClient
{
	private readonly Queue<string> responses;
	private readonly Exception? exception;
	private string lastResponse = string.Empty;

	public ScriptedChatClient(params string[] responses)
	{
		this.responses = new Queue<string>(responses);
	}

	public ScriptedChatClient(Exception exception)
	{
		responses = [];
		this.exception = exception;
	}

	public IReadOnlyList<ChatMessage>? CapturedMessages { get; private set; }

	public ChatOptions? CapturedOptions { get; private set; }

	public int InvocationCount { get; private set; }

	public string LastUserText => CapturedMessages!.Last(message => message.Role == ChatRole.User).Text;

	public Task<ChatResponse> GetResponseAsync(
		IEnumerable<ChatMessage> messages,
		ChatOptions? options = null,
		CancellationToken cancellationToken = default)
	{
		var reply = NextReply(messages, options, cancellationToken);
		return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, reply)));
	}

	public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
		IEnumerable<ChatMessage> messages,
		ChatOptions? options = null,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var reply = NextReply(messages, options, cancellationToken);
		foreach (var chunk in reply.Chunk(16))
		{
			await Task.Yield();
			yield return new ChatResponseUpdate(ChatRole.Assistant, new string(chunk));
		}
	}

	public object? GetService(Type serviceType, object? serviceKey = null) =>
		serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

	public void Dispose()
	{
	}

	private string NextReply(IEnumerable<ChatMessage> messages, ChatOptions? options, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		CapturedMessages = [.. messages];
		CapturedOptions = options;
		InvocationCount++;

		if (exception is not null) throw exception;

		if (responses.Count > 0) lastResponse = responses.Dequeue();
		return lastResponse;
	}
}
