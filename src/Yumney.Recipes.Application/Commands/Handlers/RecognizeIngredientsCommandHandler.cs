using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;

#pragma warning disable SA1601
public sealed partial class RecognizeIngredientsCommandHandler(
    IIngredientRecognitionService recognitionService,
    ILogger<RecognizeIngredientsCommandHandler> logger)
    : ICommandHandler<RecognizeIngredientsCommand, Result<RecognizedIngredientsResponseDto>>
{
    public async Task<Result<RecognizedIngredientsResponseDto>> HandleAsync(
        RecognizeIngredientsCommand command,
        CancellationToken cancellationToken = default)
    {
        var validation = PhotoValidator.Validate(command.Photo);
        if (validation.IsFailure)
        {
            LogValidationFailed(command.Photo.FileName, validation.Error!.Code);
            return Result.Failure<RecognizedIngredientsResponseDto>(validation.Error);
        }

        LogRecognizingIngredients(command.Photo.Content.Length);

        return await recognitionService.RecognizeAsync(command.Photo, cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Photo validation failed for {FileName}: {ErrorCode}")]
    private partial void LogValidationFailed(string fileName, string errorCode);

    [LoggerMessage(Level = LogLevel.Information, Message = "Recognizing ingredients from photo ({SizeBytes} bytes)")]
    private partial void LogRecognizingIngredients(long sizeBytes);
}
