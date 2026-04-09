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
        LogRecognizingIngredients(command.Photo.Content.Length);

        return await recognitionService.RecognizeAsync(command.Photo, cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Recognizing ingredients from photo ({SizeBytes} bytes)")]
    private partial void LogRecognizingIngredients(long sizeBytes);
}
