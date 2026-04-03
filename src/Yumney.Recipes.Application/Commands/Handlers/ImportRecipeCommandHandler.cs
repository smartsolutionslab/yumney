using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;

#pragma warning disable SA1601 // Partial elements should be documented (required for LoggerMessage source generation)
public sealed partial class ImportRecipeCommandHandler(
    IWebScraper scraper,
    IRecipeExtractionService extraction,
    ILogger<ImportRecipeCommandHandler> logger)
    : ICommandHandler<ImportRecipeCommand, Result<ExtractedRecipeDto>>
{
    public async Task<Result<ExtractedRecipeDto>> HandleAsync(ImportRecipeCommand command, CancellationToken cancellationToken = default)
    {
        var url = command.Url;
        LogImportAttempt(url.Value);

        var scrapeResult = await scraper.ScrapeAsync(url, cancellationToken);

        if (scrapeResult.IsFailure)
        {
            LogScrapeFailed(url.Value, scrapeResult.Error!.Code);
            return scrapeResult.Error!;
        }

        var extractResult = await extraction.ExtractAsync(scrapeResult.Value, cancellationToken);
        if (extractResult.IsFailure)
        {
            LogExtractionFailed(url.Value, extractResult.Error!.Code);
            return extractResult.Error!;
        }

        LogImportSuccess(url.Value, extractResult.Value.Title);
        return extractResult;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Recipe import requested for URL {SourceUrl}")]
    private partial void LogImportAttempt(string sourceUrl);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Scraping failed for URL {SourceUrl}: {Error}")]
    private partial void LogScrapeFailed(string sourceUrl, string error);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Extraction failed for URL {SourceUrl}: {Error}")]
    private partial void LogExtractionFailed(string sourceUrl, string error);

    [LoggerMessage(Level = LogLevel.Information, Message = "Recipe '{Title}' extracted from {SourceUrl}")]
    private partial void LogImportSuccess(string sourceUrl, string title);
}
