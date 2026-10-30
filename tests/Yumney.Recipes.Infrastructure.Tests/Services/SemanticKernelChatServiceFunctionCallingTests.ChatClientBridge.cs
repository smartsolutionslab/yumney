using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.Queries;
using SmartSolutionsLab.Yumney.Shared.AI;
using Xunit;
using Ai = Microsoft.Extensions.AI;
using DomainChat = SmartSolutionsLab.Yumney.Recipes.Domain.Chat;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.Services;

/// <summary>
/// Production runs the kernel on an <see cref="Ai.IChatClient"/> via
/// <see cref="SemanticKernelBridge.AsKernelChatCompletion"/>. Unlike the old
/// provider connectors, the tool-calling loop then comes from the bridge —
/// these tests drive that real loop end to end with a fake model.
/// </summary>
#pragma warning disable SA1601
public sealed partial class SemanticKernelChatServiceFunctionCallingTests
#pragma warning restore SA1601
{
	[Fact]
	public async Task ChatAsync_ChatClientRequestsSearchRecipes_KernelInvokesToolAndReturnsSuggestions()
	{
		var recipe = Guid.NewGuid();
		searchHandler.HandleAsync(Arg.Any<GetRecipesQuery>(), Arg.Any<CancellationToken>())
			.Returns(SuccessRecipes(recipe, "Carbonara"));
		var chatClient = new ToolCallingChatClient("search_recipes", "I found Carbonara.");
		var (chatService, _) = BuildChatService(chatClient.AsKernelChatCompletion());

		var result = await chatService.ChatAsync(DomainChat.ChatMessageContent.From("show me pasta"), [], Owner());

		result.Value.Suggestions.Select(suggestion => suggestion.RecipeIdentifier).Should().Equal(recipe);
	}

	[Fact]
	public async Task ChatAsync_ChatClientRequestsSearchRecipes_FinalReplyFollowsToolResult()
	{
		searchHandler.HandleAsync(Arg.Any<GetRecipesQuery>(), Arg.Any<CancellationToken>())
			.Returns(SuccessRecipes(Guid.NewGuid(), "Carbonara"));
		var chatClient = new ToolCallingChatClient("search_recipes", "I found Carbonara.");
		var (chatService, _) = BuildChatService(chatClient.AsKernelChatCompletion());

		var result = await chatService.ChatAsync(DomainChat.ChatMessageContent.From("show me pasta"), [], Owner());

		result.Value.Reply.Should().Be("I found Carbonara.");
	}

	/// <summary>
	/// Fake model: on the first turn requests the advertised tool whose name
	/// ends with <paramref name="toolName"/>; once a tool result is in the
	/// conversation, answers with <paramref name="finalReply"/>.
	/// </summary>
	private sealed class ToolCallingChatClient(string toolName, string finalReply) : Ai.IChatClient
	{
		public Task<Ai.ChatResponse> GetResponseAsync(
			IEnumerable<Ai.ChatMessage> messages,
			Ai.ChatOptions? options = null,
			CancellationToken cancellationToken = default)
		{
			var hasToolResult = messages.SelectMany(message => message.Contents).OfType<Ai.FunctionResultContent>().Any();
			if (hasToolResult) return Task.FromResult(new Ai.ChatResponse(new Ai.ChatMessage(Ai.ChatRole.Assistant, finalReply)));

			var tool = options!.Tools!.Single(candidate => candidate.Name.EndsWith(toolName, StringComparison.Ordinal));
			var call = new Ai.FunctionCallContent("call-1", tool.Name, new Dictionary<string, object?> { ["query"] = "pasta" });
			return Task.FromResult(new Ai.ChatResponse(new Ai.ChatMessage(Ai.ChatRole.Assistant, [call])));
		}

		public IAsyncEnumerable<Ai.ChatResponseUpdate> GetStreamingResponseAsync(
			IEnumerable<Ai.ChatMessage> messages,
			Ai.ChatOptions? options = null,
			CancellationToken cancellationToken = default) =>
			throw new NotSupportedException("Streaming not used by SemanticKernelChatService.");

		public object? GetService(Type serviceType, object? serviceKey = null) => null;

		public void Dispose()
		{
		}
	}
}
