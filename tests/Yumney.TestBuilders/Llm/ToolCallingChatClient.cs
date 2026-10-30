using Microsoft.Extensions.AI;

namespace SmartSolutionsLab.Yumney.TestBuilders.Llm;

/// <summary>
/// Test double for a tool-calling model: each request consumes the next
/// scripted turn — either function calls (which must name tools the caller
/// actually offered) or a final text reply. Meant to sit under
/// <see cref="FunctionInvokingChatClient"/>, which runs the real tool loop.
/// </summary>
public sealed class ToolCallingChatClient(params ToolCallingChatClient.Turn[] turns) : IChatClient
{
	private readonly Queue<Turn> remainingTurns = new(turns);
	private int callCounter;

	public List<ChatOptions?> CapturedOptions { get; } = [];

	public Task<ChatResponse> GetResponseAsync(
		IEnumerable<ChatMessage> messages,
		ChatOptions? options = null,
		CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		CapturedOptions.Add(options);

		if (!remainingTurns.TryDequeue(out var turn)) throw new InvalidOperationException("No scripted turn left.");
		if (turn.Text is not null) return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, turn.Text)));

		var offered = options?.Tools?.Select(tool => tool.Name).ToHashSet() ?? [];
		List<AIContent> calls = [];
		foreach (var (name, arguments) in turn.Calls)
		{
			if (!offered.Contains(name)) throw new InvalidOperationException($"Tool '{name}' was not offered to the model.");
			calls.Add(new FunctionCallContent($"call-{++callCounter}", name, arguments));
		}

		return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, calls)));
	}

	public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
		IEnumerable<ChatMessage> messages,
		ChatOptions? options = null,
		CancellationToken cancellationToken = default) =>
		throw new NotSupportedException("Streaming is not scripted.");

	public object? GetService(Type serviceType, object? serviceKey = null) =>
		serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

	public void Dispose()
	{
	}

	/// <summary>One scripted model turn.</summary>
	/// <param name="Calls">Function calls to emit; empty for a text turn.</param>
	/// <param name="Text">Final reply text; null for a tool-call turn.</param>
	public sealed record Turn(IReadOnlyList<(string Name, Dictionary<string, object?> Arguments)> Calls, string? Text)
	{
		public static Turn Call(string name, Dictionary<string, object?> arguments) => new([(name, arguments)], null);

		public static Turn CallAll(params (string Name, Dictionary<string, object?> Arguments)[] calls) => new(calls, null);

		public static Turn Reply(string text) => new([], text);
	}
}
