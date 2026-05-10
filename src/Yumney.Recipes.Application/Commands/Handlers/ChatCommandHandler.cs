using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;

#pragma warning disable SA1601
public sealed partial class ChatCommandHandler(
	IChatService chatService,
	IIntentParserService intentParser,
	ICurrentUser currentUser,
	ILogger<ChatCommandHandler> logger)
	: ICommandHandler<ChatCommand, Result<ChatResponseDto>>
{
	public async Task<Result<ChatResponseDto>> HandleAsync(ChatCommand command, CancellationToken cancellationToken = default)
	{
		var (message, history) = command;
		var owner = currentUser.AsOwner();

		var chatResult = await chatService.ChatAsync(message, history, owner, cancellationToken);
		if (chatResult.IsFailure) return chatResult;

		var actions = await ResolveActionsAsync(message.Value, cancellationToken);
		return chatResult.Value with { Actions = actions };
	}

	private async Task<IReadOnlyList<ChatActionDto>> ResolveActionsAsync(string message, CancellationToken cancellationToken)
	{
		var intentResult = await intentParser.ParseAsync(message, pageContext: null, cancellationToken);
		if (intentResult.IsFailure)
		{
			LogIntentParseFailed(intentResult.Error!.Message);
			return [];
		}

		return IntentToActionMapper.Map(intentResult.Value);
	}

	[LoggerMessage(Level = LogLevel.Debug, Message = "Intent parse failed for chat action mapping: {Reason}")]
	private partial void LogIntentParseFailed(string reason);
}
