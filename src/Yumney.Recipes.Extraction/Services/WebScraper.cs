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

		var structured = JsonLdRecipeParser.TryParse(html);
		if (structured is not null)
		{
			activity?.SetTag("scrape.source", "json-ld");
			activity?.SetStatus(ActivityStatusCode.Ok);
			LogJsonLdHit(url.Value);
			return new ScrapedContent(string.Empty, url, structured);
		}

		activity?.SetTag("scrape.source", "text");
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

	// Schema.org hints first: any element tagged as a Recipe (microdata
	// itemtype or RDFa typeof) usually contains exactly the ingredient
	// list and instructions. itemprop covers fragmented markup that
	// doesn't wrap the whole recipe in a single container. Fall back to
	// the largest semantic-content region, then the body.
#pragma warning disable SA1311
	private static readonly string[] contentSelectors =
	[
		"[itemtype*=\"schema.org/Recipe\" i]",
		"[typeof*=\"Recipe\" i]",
		"[itemprop*=\"recipe\" i]",
		"main",
		"article",
		"body",
	];
#pragma warning restore SA1311

	private static async Task<string> CleanHtmlAsync(string html, CancellationToken cancellationToken)
	{
		IConfiguration config = Configuration.Default;
		using var context = BrowsingContext.New(config);
		using var document = await context.OpenAsync(req => req.Content(html), cancellationToken);

		foreach (var element in document.QuerySelectorAll(removeSelector).ToList())
		{
			element.Remove();
		}

		foreach (var selector in contentSelectors)
		{
			var matches = document.QuerySelectorAll(selector);
			if (matches.Length == 0) continue;

			// Schema.org hints can surface multiple fragments (one per itemprop) —
			// concatenate them in document order so nothing is lost.
			var combined = string.Join("\n", matches.Select(m => m.TextContent.Trim())).Trim();
			if (combined.Length > 0) return combined;
		}

		return string.Empty;
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

	[LoggerMessage(Level = LogLevel.Information, Message = "JSON-LD Recipe found for URL {SourceUrl}; skipping LLM extraction")]
	private partial void LogJsonLdHit(string sourceUrl);
}
