using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;

#pragma warning disable SA1601
public sealed partial class ChatCommandHandler(
    IChatService chatService,
    ICurrentUser currentUser,
    ILogger<ChatCommandHandler> logger) : ICommandHandler<ChatCommand, Result<ChatResponseDto>>
{
    public async Task<Result<ChatResponseDto>> HandleAsync(ChatCommand command, CancellationToken cancellationToken = default)
    {
        var owner = OwnerIdentifier.From(currentUser.UserId);

        LogChatRequest(owner.Value, command.Message.Length, command.History.Count);

        return await chatService.ChatAsync(command.Message, command.History, owner, cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Chat request from {UserId}, message length {MessageLength}, history {HistoryCount}")]
    private partial void LogChatRequest(string userId, int messageLength, int historyCount);
}
