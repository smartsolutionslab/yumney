using System.Diagnostics;
using AngleSharp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.Services;

#pragma warning disable SA1601
#pragma warning disable SA1303
public sealed partial class WebScraper(HttpClient httpClient, IOptions<ScrapingOptions> scrapingOptions, ILogger<WebScraper> logger) : IWebScraper
{
    private const string removeSelector = "script, style, nav, footer, header, aside, iframe, noscript";

    private readonly ScrapingOptions options = scrapingOptions.Value;

    public async Task<Result<ScrapedContent>> ScrapeAsync(RecipeUrl url, CancellationToken cancellationToken = default)
    {
        using var activity = ExtractionDiagnostics.ActivitySource.StartActivity("scrape.webpage");
        activity?.SetTag("scrape.url", url.Value);

        string html;
        try
        {
            using var response = await httpClient.GetAsync(url.Value, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var contentLength = response.Content.Headers.ContentLength;
            if (contentLength > options.MaxRawHtmlLength)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Content too large");
                LogContentTooLarge(url.Value, (int)contentLength.Value, options.MaxRawHtmlLength);
                return ImportRecipeErrors.ContentTooLarge;
            }

            html = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Timeout");
            LogScrapeTimeout(url.Value);
            return ImportRecipeErrors.ScrapeTimeout;
        }
        catch (HttpRequestException ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            LogPageUnreachable(url.Value, ex.Message);
            return ImportRecipeErrors.PageUnreachable;
        }

        activity?.SetTag("scrape.html_length", html.Length);

        if (html.Length > options.MaxRawHtmlLength)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Content too large");
            LogContentTooLarge(url.Value, html.Length, options.MaxRawHtmlLength);
            return ImportRecipeErrors.ContentTooLarge;
        }

        var cleanedText = await CleanHtmlAsync(html, cancellationToken);

        if (!cleanedText.HasValue())
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Empty content");
            LogEmptyContent(url.Value);
            return ImportRecipeErrors.NoRecipeFound;
        }

        if (cleanedText.Length > options.MaxContentLength)
        {
            LogContentTruncated(url.Value, cleanedText.Length, options.MaxContentLength);
            cleanedText = TruncateAtWordBoundary(cleanedText, options.MaxContentLength);
        }

        activity?.SetTag("scrape.cleaned_length", cleanedText.Length);
        activity?.SetStatus(ActivityStatusCode.Ok);
        return new ScrapedContent(cleanedText, url);
    }

    private static async Task<string> CleanHtmlAsync(string html, CancellationToken cancellationToken)
    {
        IConfiguration config = Configuration.Default;
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

    private static string TruncateAtWordBoundary(string text, int maxLength)
    {
        if (text.Length <= maxLength) return text;

        var lastSpace = text.LastIndexOf(' ', maxLength);
        return lastSpace > 0 ? text[..lastSpace] : text[..maxLength];
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Scrape timed out for URL {SourceUrl}")]
    private partial void LogScrapeTimeout(string sourceUrl);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Page unreachable at URL {SourceUrl}: {Reason}")]
    private partial void LogPageUnreachable(string sourceUrl, string reason);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No content extracted from URL {SourceUrl}")]
    private partial void LogEmptyContent(string sourceUrl);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Raw HTML too large for URL {SourceUrl}: {ActualLength} chars exceeds limit of {MaxLength}")]
    private partial void LogContentTooLarge(string sourceUrl, int actualLength, int maxLength);

    [LoggerMessage(Level = LogLevel.Information, Message = "Content truncated for URL {SourceUrl}: {OriginalLength} chars truncated to {MaxLength}")]
    private partial void LogContentTruncated(string sourceUrl, int originalLength, int maxLength);
}
