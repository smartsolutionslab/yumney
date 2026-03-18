using AngleSharp;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Services;

#pragma warning disable SA1601
#pragma warning disable SA1303
public sealed partial class WebScraper(HttpClient httpClient, ILogger<WebScraper> logger)
    : IWebScraper
{
    private const string removeSelector = "script, style, nav, footer, header, aside, iframe, noscript";

    public async Task<Result<ScrapedContent>> ScrapeAsync(RecipeUrl url, CancellationToken cancellationToken = default)
    {
        string html;
        try
        {
            html = await httpClient.GetStringAsync(url.Value, cancellationToken);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            LogScrapeTimeout(url.Value);
            return Result<ScrapedContent>.Failure(ImportRecipeErrors.ScrapeTimeout);
        }
        catch (HttpRequestException ex)
        {
            LogPageUnreachable(url.Value, ex.Message);
            return Result<ScrapedContent>.Failure(ImportRecipeErrors.PageUnreachable);
        }

        var cleanedText = await CleanHtmlAsync(html, cancellationToken);

        if (string.IsNullOrWhiteSpace(cleanedText))
        {
            LogEmptyContent(url.Value);
            return Result<ScrapedContent>.Failure(ImportRecipeErrors.NoRecipeFound);
        }

        return Result<ScrapedContent>.Success(new ScrapedContent(cleanedText, url));
    }

    private static async Task<string> CleanHtmlAsync(string html, CancellationToken cancellationToken)
    {
        var config = Configuration.Default;
        using var context = BrowsingContext.New(config);
        using var document = await context.OpenAsync(req => req.Content(html), cancellationToken);

        foreach (var element in document.QuerySelectorAll(removeSelector).ToList())
        {
            element.Remove();
        }

        var contentElement = document.QuerySelector("main")
            ?? document.QuerySelector("article")
            ?? document.QuerySelector("body");

        return contentElement?.TextContent.Trim() ?? string.Empty;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Scrape timed out for URL {SourceUrl}")]
    private partial void LogScrapeTimeout(string sourceUrl);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Page unreachable at URL {SourceUrl}: {Reason}")]
    private partial void LogPageUnreachable(string sourceUrl, string reason);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No content extracted from URL {SourceUrl}")]
    private partial void LogEmptyContent(string sourceUrl);
}
