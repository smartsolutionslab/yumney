using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;

#pragma warning disable SA1601
#pragma warning disable SA1303
#pragma warning disable SA1311
public sealed partial class ImportRecipeFromPhotosCommandHandler(
    IRecipeExtractionService extractionService,
    ILogger<ImportRecipeFromPhotosCommandHandler> logger)
    : ICommandHandler<ImportRecipeFromPhotosCommand, Result<ExtractedRecipeDto>>
{
    private const int maxPhotos = 10;
    private const long maxPhotoSizeBytes = 10 * 1024 * 1024;

    private static readonly HashSet<string> allowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp",
    ];

    public async Task<Result<ExtractedRecipeDto>> HandleAsync(
        ImportRecipeFromPhotosCommand command,
        CancellationToken cancellationToken = default)
    {
        var photos = command.Photos;

        if (photos.Count == 0 || photos.Count > maxPhotos) return Result<ExtractedRecipeDto>.Failure(ImportRecipeErrors.TooManyPhotos);

        foreach (var photo in photos)
        {
            if (photo.Content.Length > maxPhotoSizeBytes)
            {
                LogPhotoTooLarge(photo.FileName, photo.Content.Length);
                return Result<ExtractedRecipeDto>.Failure(ImportRecipeErrors.PhotoTooLarge);
            }

            if (!allowedContentTypes.Contains(photo.ContentType.ToLowerInvariant()))
            {
                LogInvalidFormat(photo.FileName, photo.ContentType);
                return Result<ExtractedRecipeDto>.Failure(ImportRecipeErrors.InvalidPhotoFormat);
            }
        }

        LogExtractingFromPhotos(photos.Count);

        return await extractionService.ExtractFromPhotosAsync(photos, cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Photo {FileName} exceeds size limit: {SizeBytes} bytes")]
    private partial void LogPhotoTooLarge(string fileName, long sizeBytes);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid photo format {FileName}: {ContentType}")]
    private partial void LogInvalidFormat(string fileName, string contentType);

    [LoggerMessage(Level = LogLevel.Information, Message = "Extracting recipe from {PhotoCount} photos")]
    private partial void LogExtractingFromPhotos(int photoCount);
}
