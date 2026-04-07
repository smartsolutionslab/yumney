using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;

#pragma warning disable SA1601
#pragma warning disable SA1303
#pragma warning disable SA1311
public sealed partial class RecognizeIngredientsCommandHandler(
    IIngredientRecognitionService recognitionService,
    ILogger<RecognizeIngredientsCommandHandler> logger)
    : ICommandHandler<RecognizeIngredientsCommand, Result<RecognizedIngredientsResponseDto>>
{
    private const long maxPhotoSizeBytes = 10 * 1024 * 1024;

    private static readonly HashSet<string> allowedContentTypes =
    [
        MediaTypes.ImageJpeg,
        MediaTypes.ImagePng,
        MediaTypes.ImageWebp,
    ];

    public async Task<Result<RecognizedIngredientsResponseDto>> HandleAsync(
        RecognizeIngredientsCommand command,
        CancellationToken cancellationToken = default)
    {
        var photo = command.Photo;

        if (photo.Content.Length > maxPhotoSizeBytes)
        {
            LogPhotoTooLarge(photo.FileName, photo.Content.Length);
            return ImportRecipeErrors.PhotoTooLarge;
        }

        if (!allowedContentTypes.Contains(photo.ContentType.ToLowerInvariant()))
        {
            LogInvalidFormat(photo.FileName, photo.ContentType);
            return ImportRecipeErrors.InvalidPhotoFormat;
        }

        LogRecognizingIngredients(photo.Content.Length);

        return await recognitionService.RecognizeAsync(photo, cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Recognition photo too large: {FileName} ({SizeBytes} bytes)")]
    private partial void LogPhotoTooLarge(string fileName, long sizeBytes);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Recognition photo invalid format: {FileName} ({ContentType})")]
    private partial void LogInvalidFormat(string fileName, string contentType);

    [LoggerMessage(Level = LogLevel.Information, Message = "Recognizing ingredients from photo ({SizeBytes} bytes)")]
    private partial void LogRecognizingIngredients(long sizeBytes);
}
