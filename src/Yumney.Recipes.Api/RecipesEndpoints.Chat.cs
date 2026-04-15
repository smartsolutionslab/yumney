using FluentValidation;
using SmartSolutionsLab.Yumney.Recipes.Api.Requests;
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
    private static async Task<IResult> ChatAsync(
        ChatRequestDto request,
        IValidator<ChatRequestDto> validator,
        ICommandHandler<ChatCommand, Result<ChatResponseDto>> handler,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (validation.HasFailed()) return validation.ToValidationProblem();

        var (message, history) = request;

        var command = new ChatCommand(
            ChatMessageContent.From(message),
            history.MapToChatHistoryEntries().ToList());

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToOk();
    }

    private static async Task<IResult> ParseIntentAsync(
        ParseIntentRequestDto request,
        IValidator<ParseIntentRequestDto> validator,
        ICommandHandler<ParseIntentCommand, Result<ParsedIntentDto>> handler,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (validation.HasFailed()) return validation.ToValidationProblem();

        var command = new ParseIntentCommand(request.Message, request.Context);

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToOk();
    }

    private static async Task<IResult> ImportFromTextAsync(
        ImportFromTextRequestDto request,
        IValidator<ImportFromTextRequestDto> validator,
        ICommandHandler<ImportRecipeFromTextCommand, Result<ExtractedRecipeDto>> handler,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (validation.HasFailed()) return validation.ToValidationProblem();

        var command = new ImportRecipeFromTextCommand(request.Text);

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToOk();
    }
}
