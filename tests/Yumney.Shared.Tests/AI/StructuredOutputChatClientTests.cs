using FluentAssertions;
using Microsoft.Extensions.AI;
using SmartSolutionsLab.Yumney.Shared.AI;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.AI;

public class StructuredOutputChatClientTests
{
	[Fact]
	public async Task GetResponseAsync_JsonObjectMode_SendsPlainJsonFormat()
	{
		var inner = new CapturingChatClient();

		await new StructuredOutputChatClient(inner, StructuredOutputMode.JsonObject).GetResponseAsync("hi", SchemaOptions());

		inner.CapturedOptions!.ResponseFormat.Should().Be(ChatResponseFormat.Json);
	}

	[Fact]
	public async Task GetResponseAsync_NoneMode_SendsNoFormat()
	{
		var inner = new CapturingChatClient();

		await new StructuredOutputChatClient(inner, StructuredOutputMode.None).GetResponseAsync("hi", SchemaOptions());

		inner.CapturedOptions!.ResponseFormat.Should().BeNull();
	}

	[Fact]
	public async Task GetResponseAsync_PlainJsonRequest_IsLeftUnchanged()
	{
		var inner = new CapturingChatClient();
		var options = new ChatOptions { ResponseFormat = ChatResponseFormat.Json };

		await new StructuredOutputChatClient(inner, StructuredOutputMode.None).GetResponseAsync("hi", options);

		inner.CapturedOptions.Should().BeSameAs(options);
	}

	[Fact]
	public async Task GetResponseAsync_DowngradedRequest_DoesNotMutateCallerOptions()
	{
		var options = SchemaOptions();

		await new StructuredOutputChatClient(new CapturingChatClient(), StructuredOutputMode.None).GetResponseAsync("hi", options);

		options.ResponseFormat.Should().BeOfType<ChatResponseFormatJson>().Which.Schema.Should().NotBeNull();
	}

	[Fact]
	public async Task GetStreamingResponseAsync_JsonObjectMode_SendsPlainJsonFormat()
	{
		var inner = new CapturingChatClient();

		var updates = new StructuredOutputChatClient(inner, StructuredOutputMode.JsonObject).GetStreamingResponseAsync("hi", SchemaOptions());
		await updates.ToListAsync();

		inner.CapturedOptions!.ResponseFormat.Should().Be(ChatResponseFormat.Json);
	}

	private static ChatOptions SchemaOptions() => LlmJson.OptionsFor<Sample>();

	private sealed record Sample(string Title);

	private sealed class CapturingChatClient : IChatClient
	{
		public ChatOptions? CapturedOptions { get; private set; }

		public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
		{
			CapturedOptions = options;
			return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "{}")));
		}

		public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
			IEnumerable<ChatMessage> messages,
			ChatOptions? options = null,
			[System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			CapturedOptions = options;
			await Task.Yield();
			yield return new ChatResponseUpdate(ChatRole.Assistant, "{}");
		}

		public object? GetService(Type serviceType, object? serviceKey = null) => null;

		public void Dispose()
		{
		}
	}
}
