using System.Runtime.CompilerServices;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.Common;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Api.Tests;

#pragma warning disable SA1311
public class ImportStreamSseTests
{
	private static readonly string compactJson =
		"""{"title":"Lasagne","ingredients":[{"name":"Pasta","amount":500,"unit":"g"}],"steps":[{"number":1,"description":"Cook"}]}""";

	private static readonly string prettyJson = """
        {
          "title": "Lasagne",
          "ingredients": [
            { "name": "Pasta", "amount": 500, "unit": "g" }
          ],
          "steps": [
            { "number": 1, "description": "Cook" }
          ]
        }
        """;

	[Fact]
	public void ExtractJson_PlainJson_ReturnsUnchanged()
	{
		var result = LlmResponseParser.ExtractJson(compactJson);

		result.Should().Be(compactJson);
	}

	[Fact]
	public void ExtractJson_JsonWrappedInMarkdownFence_StripsFence()
	{
		var wrapped = $"```json\n{compactJson}\n```";

		var result = LlmResponseParser.ExtractJson(wrapped);

		result.Should().Be(compactJson);
	}

	[Fact]
	public void ExtractJson_JsonWrappedInPlainFence_StripsFence()
	{
		var wrapped = $"```\n{compactJson}\n```";

		var result = LlmResponseParser.ExtractJson(wrapped);

		result.Should().Be(compactJson);
	}

	[Fact]
	public void ExtractJson_FenceWithLeadingWhitespace_StripsFenceAndTrims()
	{
		var wrapped = $"  ```json\n{compactJson}\n```  ";

		var result = LlmResponseParser.ExtractJson(wrapped);

		result.Should().Be(compactJson);
	}

	[Fact]
	public void ExtractJson_CaseInsensitiveJsonFence_StripsFence()
	{
		var wrapped = $"```JSON\n{compactJson}\n```";

		var result = LlmResponseParser.ExtractJson(wrapped);

		result.Should().Be(compactJson);
	}

	[Fact]
	public void CompactJson_PrettyPrintedJson_ReturnsSingleLine()
	{
		var result = RecipesEndpoints.CompactJson(prettyJson);

		result.Should().NotContain("\n");
		result.Should().NotContain("\r");
		result.Should().Contain("\"title\":\"Lasagne\"");
		result.Should().Contain("\"name\":\"Pasta\"");
	}

	[Fact]
	public void CompactJson_AlreadyCompact_ReturnsEquivalentJson()
	{
		var result = RecipesEndpoints.CompactJson(compactJson);

		result.Should().Be(compactJson);
	}

	[Fact]
	public async Task ImportStreamAsync_PrettyPrintedLlmOutput_EmitsSingleLineDoneEvent()
	{
		var (body, scraper, extraction) = SetupStreamTest(prettyJson);

		await InvokeImportStreamAsync("https://example.com/recipe", scraper, extraction, body);

		var output = Encoding.UTF8.GetString(body.ToArray());
		var doneEvent = ParseSseEvents(output).First(evt => evt.Type == "done");

		doneEvent.Data.Should().NotContain("\n");
		doneEvent.Data.Should().Contain("\"title\":\"Lasagne\"");
	}

	[Fact]
	public async Task ImportStreamAsync_FencedLlmOutput_StripsMarkdownFence()
	{
		var fenced = $"```json\n{prettyJson}\n```";
		var (body, scraper, extraction) = SetupStreamTest(fenced);

		await InvokeImportStreamAsync("https://example.com/recipe", scraper, extraction, body);

		var output = Encoding.UTF8.GetString(body.ToArray());
		var doneEvent = ParseSseEvents(output).First(evt => evt.Type == "done");

		doneEvent.Data.Should().NotContain("```");
		doneEvent.Data.Should().Contain("\"title\":\"Lasagne\"");
	}

	[Fact]
	public async Task ImportStreamAsync_ValidFlow_EmitsStatusChunkAndDoneEvents()
	{
		var (body, scraper, extraction) = SetupStreamTest(compactJson);

		await InvokeImportStreamAsync("https://example.com/recipe", scraper, extraction, body);

		var output = Encoding.UTF8.GetString(body.ToArray());
		var events = ParseSseEvents(output);

		events.Should().Contain(e => e.Type == "status" && e.Data == "Fetching page...");
		events.Should().Contain(e => e.Type == "status" && e.Data == "Extracting recipe...");
		events.Should().Contain(e => e.Type == "chunk");
		events.Should().Contain(e => e.Type == "done");
	}

	[Fact]
	public async Task ImportStreamAsync_InvalidUrl_EmitsFailEvent()
	{
		var scraper = Substitute.For<IWebScraper>();
		var extraction = Substitute.For<IRecipeExtractionService>();
		var body = new MemoryStream();

		await InvokeImportStreamAsync("not-a-url", scraper, extraction, body);

		var output = Encoding.UTF8.GetString(body.ToArray());
		var events = ParseSseEvents(output);

		events.Should().Contain(e => e.Type == "fail" && e.Data == "Invalid URL");
	}

	[Fact]
	public async Task ImportStreamAsync_ScrapeFailure_EmitsFailEvent()
	{
		var scraper = Substitute.For<IWebScraper>();
		scraper.ScrapeAsync(Arg.Any<RecipeUrl>(), Arg.Any<CancellationToken>())
			.Returns(Result<ScrapedContent>.Failure(new ApiError("Scrape.Failed", "Page unreachable", 502)));
		var extraction = Substitute.For<IRecipeExtractionService>();
		var body = new MemoryStream();

		await InvokeImportStreamAsync("https://example.com/recipe", scraper, extraction, body);

		var output = Encoding.UTF8.GetString(body.ToArray());
		var events = ParseSseEvents(output);

		events.Should().Contain(e => e.Type == "fail" && e.Data == "Page unreachable");
	}

	[Fact]
	public async Task ImportStreamAsync_ExtractionThrows_EmitsFailEvent()
	{
		var scraper = Substitute.For<IWebScraper>();
		scraper.ScrapeAsync(Arg.Any<RecipeUrl>(), Arg.Any<CancellationToken>())
			.Returns(Result<ScrapedContent>.Success(
				new ScrapedContent("content", RecipeUrl.From("https://example.com/recipe"))));

		var extraction = Substitute.For<IRecipeExtractionService>();
		extraction.StreamExtractAsync(Arg.Any<ScrapedContent>(), Arg.Any<CancellationToken>())
			.Returns(ThrowingAsyncEnumerable());

		var body = new MemoryStream();

		await InvokeImportStreamAsync("https://example.com/recipe", scraper, extraction, body);

		var output = Encoding.UTF8.GetString(body.ToArray());
		var events = ParseSseEvents(output);

		events.Should().Contain(e => e.Type == "fail" && e.Data == "Extraction failed");
	}

	private static (MemoryStream Body, IWebScraper Scraper, IRecipeExtractionService Extraction) SetupStreamTest(
		string llmOutput)
	{
		var scraper = Substitute.For<IWebScraper>();
		scraper.ScrapeAsync(Arg.Any<RecipeUrl>(), Arg.Any<CancellationToken>())
			.Returns(Result<ScrapedContent>.Success(
				new ScrapedContent("scraped content", RecipeUrl.From("https://example.com/recipe"))));

		var extraction = Substitute.For<IRecipeExtractionService>();
		extraction.StreamExtractAsync(Arg.Any<ScrapedContent>(), Arg.Any<CancellationToken>())
			.Returns(ToAsyncEnumerable(llmOutput));

		return (new MemoryStream(), scraper, extraction);
	}

	private static async Task InvokeImportStreamAsync(
		string url,
		IWebScraper scraper,
		IRecipeExtractionService extraction,
		MemoryStream body)
	{
		var httpContext = new DefaultHttpContext();
		httpContext.Response.Body = body;

		await RecipesEndpoints.ImportStreamAsync(httpContext, url, scraper, extraction, NullLogger<Program>.Instance, CancellationToken.None);

		body.Position = 0;
	}

	private static List<SseEvent> ParseSseEvents(string raw)
	{
		List<SseEvent> events = [];
		string? currentType = null;

		foreach (var line in raw.Split('\n'))
		{
			if (line.StartsWith("event: ", StringComparison.Ordinal))
			{
				currentType = line[7..].Trim();
			}
			else if (line.StartsWith("data: ", StringComparison.Ordinal) && currentType is not null)
			{
				events.Add(new SseEvent(currentType, line[6..]));
				currentType = null;
			}
		}

		return events;
	}

	private static async IAsyncEnumerable<string> ToAsyncEnumerable(
		string content,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;

		// Simulate chunked streaming by yielding character groups
		for (var i = 0; i < content.Length; i += 50)
		{
			cancellationToken.ThrowIfCancellationRequested();
			yield return content[i..Math.Min(i + 50, content.Length)];
		}
	}

	private static async IAsyncEnumerable<string> ThrowingAsyncEnumerable(
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
		yield return "partial";
		throw new InvalidOperationException("LLM failed");
	}

	private sealed record SseEvent(string Type, string Data);
}
