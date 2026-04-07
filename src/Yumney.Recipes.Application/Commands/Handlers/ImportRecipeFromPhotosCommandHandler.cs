using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;

#pragma warning disable SA1601
public sealed partial class ImportRecipeFromPhotosCommandHandler(IRecipeExtractionService extractionService, ILogger<ImportRecipeFromPhotosCommandHandler> logger)
    : ICommandHandler<ImportRecipeFromPhotosCommand, Result<ExtractedRecipeDto>>
{
    public async Task<Result<ExtractedRecipeDto>> HandleAsync(ImportRecipeFromPhotosCommand command, CancellationToken cancellationToken = default)
    {
        var photos = command.Photos;

        var validation = PhotoValidator.ValidateMany(photos, PhotoValidator.MaxPhotos);
        if (validation.IsFailure)
        {
            LogValidationFailed(validation.Error!.Code);
            return Result.Failure<ExtractedRecipeDto>(validation.Error);
        }

        LogExtractingFromPhotos(photos.Count);

        return await extractionService.ExtractFromPhotosAsync(photos, cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Photo validation failed: {ErrorCode}")]
    private partial void LogValidationFailed(string errorCode);

    [LoggerMessage(Level = LogLevel.Information, Message = "Extracting recipe from {PhotoCount} photos")]
    private partial void LogExtractingFromPhotos(int photoCount);
}
