using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Domain.Chat;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Tests.Commands;

public class ChatCommandHandlerTests
{
	private readonly IChatService chatService = Substitute.For<IChatService>();
	private readonly IIntentParserService intentParser = Substitute.For<IIntentParserService>();
	private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
	private readonly ChatCommandHandler handler;

	public ChatCommandHandlerTests()
	{
		currentUser.UserId.Returns("user-123");
		intentParser.ParseAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
			.Returns(Result<ParsedIntentDto>.Success(new ParsedIntentDto("general_chat", new(), null)));
		handler = new ChatCommandHandler(chatService, intentParser, currentUser, NullLogger<ChatCommandHandler>.Instance);
	}

	[Fact]
	public async Task HandleAsync_DelegatesToChatService()
	{
		var message = ChatMessageContent.From("What can I cook?");
		var history = new List<ChatHistoryEntry>();
		var expectedResponse = new ChatResponseDto("Try pasta!", [], []);

		chatService.ChatAsync(message, history, Arg.Any<OwnerIdentifier>(), Arg.Any<CancellationToken>())
			.Returns(Result<ChatResponseDto>.Success(expectedResponse));

		var result = await handler.HandleAsync(new ChatCommand(message, history));

		result.IsSuccess.Should().BeTrue();
		result.Value.Reply.Should().Be("Try pasta!");
	}

	[Fact]
	public async Task HandleAsync_PassesOwnerFromCurrentUser()
	{
		var message = ChatMessageContent.From("Hello");
		var history = new List<ChatHistoryEntry>();

		chatService.ChatAsync(
				Arg.Any<ChatMessageContent>(),
				Arg.Any<IReadOnlyList<ChatHistoryEntry>>(),
				Arg.Any<OwnerIdentifier>(),
				Arg.Any<CancellationToken>())
			.Returns(Result<ChatResponseDto>.Success(new ChatResponseDto("Hi!", [], [])));

		await handler.HandleAsync(new ChatCommand(message, history));

		await chatService.Received(1).ChatAsync(
			message,
			history,
			Arg.Is<OwnerIdentifier>(o => o.Value == "user-123"),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_WithHistory_ForwardsHistory()
	{
		var message = ChatMessageContent.From("More ideas?");
		var history = new List<ChatHistoryEntry>
		{
			new(ChatRole.User, ChatMessageContent.From("What can I cook?")),
			new(ChatRole.Assistant, ChatMessageContent.From("Try pasta")),
		};

		chatService.ChatAsync(
				Arg.Any<ChatMessageContent>(),
				Arg.Any<IReadOnlyList<ChatHistoryEntry>>(),
				Arg.Any<OwnerIdentifier>(),
				Arg.Any<CancellationToken>())
			.Returns(Result<ChatResponseDto>.Success(new ChatResponseDto("Try risotto!", [], [])));

		var result = await handler.HandleAsync(new ChatCommand(message, history));

		result.IsSuccess.Should().BeTrue();
		await chatService.Received(1).ChatAsync(
			message,
			Arg.Is<IReadOnlyList<ChatHistoryEntry>>(h => h.Count == 2),
			Arg.Any<OwnerIdentifier>(),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_ServiceReturnsFailure_PropagatesFailure()
	{
		var message = ChatMessageContent.From("Test");
		var error = new ApiError("CHAT_FAILED", "Chat failed", 500);

		chatService.ChatAsync(
				Arg.Any<ChatMessageContent>(),
				Arg.Any<IReadOnlyList<ChatHistoryEntry>>(),
				Arg.Any<OwnerIdentifier>(),
				Arg.Any<CancellationToken>())
			.Returns(Result<ChatResponseDto>.Failure(error));

		var result = await handler.HandleAsync(new ChatCommand(message, []));

		result.IsSuccess.Should().BeFalse();
		result.Error!.Code.Should().Be("CHAT_FAILED");
	}

	[Fact]
	public async Task HandleAsync_NavigateIntent_AppendsNavigateAction()
	{
		var message = ChatMessageContent.From("Open shopping list");
		chatService.ChatAsync(message, Arg.Any<IReadOnlyList<ChatHistoryEntry>>(), Arg.Any<OwnerIdentifier>(), Arg.Any<CancellationToken>())
			.Returns(Result<ChatResponseDto>.Success(new ChatResponseDto("Sure!", [], [])));
		intentParser.ParseAsync("Open shopping list", null, Arg.Any<CancellationToken>())
			.Returns(Result<ParsedIntentDto>.Success(new ParsedIntentDto(
				"navigate",
				new() { ["target"] = "shopping-list" },
				null)));

		var result = await handler.HandleAsync(new ChatCommand(message, []));

		result.IsSuccess.Should().BeTrue();
		result.Value.Actions.Should().ContainSingle();
		result.Value.Actions[0].Type.Should().Be(ChatActionType.Navigate);
		result.Value.Actions[0].Route.Should().Be("/shopping");
	}

	[Fact]
	public async Task HandleAsync_GeneralChatIntent_ReturnsNoActions()
	{
		var message = ChatMessageContent.From("How do I poach an egg?");
		chatService.ChatAsync(message, Arg.Any<IReadOnlyList<ChatHistoryEntry>>(), Arg.Any<OwnerIdentifier>(), Arg.Any<CancellationToken>())
			.Returns(Result<ChatResponseDto>.Success(new ChatResponseDto("Bring water to a simmer…", [], [])));
		intentParser.ParseAsync("How do I poach an egg?", null, Arg.Any<CancellationToken>())
			.Returns(Result<ParsedIntentDto>.Success(new ParsedIntentDto("general_chat", new(), null)));

		var result = await handler.HandleAsync(new ChatCommand(message, []));

		result.IsSuccess.Should().BeTrue();
		result.Value.Actions.Should().BeEmpty();
	}

	[Fact]
	public async Task HandleAsync_IntentParserFails_ReturnsReplyWithEmptyActions()
	{
		var message = ChatMessageContent.From("Open shopping list");
		chatService.ChatAsync(message, Arg.Any<IReadOnlyList<ChatHistoryEntry>>(), Arg.Any<OwnerIdentifier>(), Arg.Any<CancellationToken>())
			.Returns(Result<ChatResponseDto>.Success(new ChatResponseDto("Sure!", [], [])));
		intentParser.ParseAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
			.Returns(Result<ParsedIntentDto>.Failure(new ApiError("INTENT_FAILED", "boom", 500)));

		var result = await handler.HandleAsync(new ChatCommand(message, []));

		result.IsSuccess.Should().BeTrue();
		result.Value.Reply.Should().Be("Sure!");
		result.Value.Actions.Should().BeEmpty();
	}
}
