using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;

#pragma warning disable SA1601
public sealed partial class ImportRecipeFromTextCommandHandler(
    IRecipeExtractionService extraction,
    ILogger<ImportRecipeFromTextCommandHandler> logger)
    : ICommandHandler<ImportRecipeFromTextCommand, Result<ExtractedRecipeDto>>
{
    public async Task<Result<ExtractedRecipeDto>> HandleAsync(ImportRecipeFromTextCommand command, CancellationToken cancellationToken = default)
    {
        var text = command.RecipeText;

        LogTextImportAttempt(text.Length);

        var content = new ScrapedContent(text, SourceUrl: string.Empty);
        var extractResult = await extraction.ExtractAsync(content, cancellationToken);

        if (extractResult.IsFailure)
        {
            LogTextExtractionFailed(extractResult.Error!.Code);
            return extractResult.Error!;
        }

        LogTextImportSuccess(extractResult.Value.Title);
        return extractResult;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Recipe import from text requested, length {TextLength}")]
    private partial void LogTextImportAttempt(int textLength);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Text extraction failed: {Error}")]
    private partial void LogTextExtractionFailed(string error);

    [LoggerMessage(Level = LogLevel.Information, Message = "Recipe '{Title}' extracted from pasted text")]
    private partial void LogTextImportSuccess(string title);
}
