using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Domain.Chat;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Web;

namespace SmartSolutionsLab.Yumney.Recipes.Api;

#pragma warning disable SA1601
public static partial class RecipesEndpoints
{
#pragma warning disable SA1303
    private const string emptyChatMessageError = "Message cannot be empty.";
#pragma warning restore SA1303

    private static async Task<IResult> ChatAsync(
        ChatRequestDto request,
        ICommandHandler<ChatCommand, Result<ChatResponseDto>> handler,
        CancellationToken cancellationToken)
    {
        var (message, history) = request;

        if (string.IsNullOrWhiteSpace(message))
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, detail: emptyChatMessageError);

        var command = new ChatCommand(
            ChatMessageContent.From(message),
            history.MapToChatHistoryEntries().ToList());

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToOk();
    }

    private static async Task<IResult> ParseIntentAsync(
        ParseIntentRequestDto request,
        ICommandHandler<ParseIntentCommand, Result<ParsedIntentDto>> handler,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, detail: emptyChatMessageError);

        var command = new ParseIntentCommand(request.Message, request.Context);

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToOk();
    }
}
