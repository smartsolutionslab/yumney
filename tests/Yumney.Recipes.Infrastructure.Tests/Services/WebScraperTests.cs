using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.Services;

#pragma warning disable SA1202
public class WebScraperTests
{
    private readonly ILogger<WebScraper> logger = Substitute.For<ILogger<WebScraper>>();

    private static IOptions<ScrapingOptions> CreateOptions(
        int maxContentLength = 12_000,
        int maxRawHtmlLength = 500_000)
    {
        var options = Substitute.For<IOptions<ScrapingOptions>>();
        options.Value.Returns(new ScrapingOptions
        {
            MaxContentLength = maxContentLength,
            MaxRawHtmlLength = maxRawHtmlLength,
        });
        return options;
    }

    [Fact]
    public async Task ScrapeAsync_ValidHtml_ReturnsCleanedText()
    {
        var html = "<html><body><main><h1>Recipe</h1><p>Delicious pasta</p></main></body></html>";
        var httpClient = CreateHttpClient(html);
        var sut = new WebScraper(httpClient, CreateOptions(), logger);

        var result = await sut.ScrapeAsync(RecipeUrl.From("https://example.com/recipe"));

        result.IsSuccess.Should().BeTrue();
        result.Value.CleanedText.Should().Contain("Recipe");
        result.Value.CleanedText.Should().Contain("Delicious pasta");
        result.Value.SourceUrl.Should().Be(RecipeUrl.From("https://example.com/recipe"));
    }

    [Fact]
    public async Task ScrapeAsync_RemovesScriptAndStyleTags()
    {
        var html = """
            <html><body>
            <script>alert('xss')</script>
            <style>.hidden { display: none; }</style>
            <main><p>Recipe content</p></main>
            </body></html>
            """;
        var httpClient = CreateHttpClient(html);
        var sut = new WebScraper(httpClient, CreateOptions(), logger);

        var result = await sut.ScrapeAsync(RecipeUrl.From("https://example.com/recipe"));

        result.IsSuccess.Should().BeTrue();
        result.Value.CleanedText.Should().NotContain("alert");
        result.Value.CleanedText.Should().NotContain("display: none");
        result.Value.CleanedText.Should().Contain("Recipe content");
    }

    [Fact]
    public async Task ScrapeAsync_RemovesNavFooterHeaderAside()
    {
        var html = """
            <html><body>
            <nav>Menu items</nav>
            <header>Site header</header>
            <main><p>Recipe content</p></main>
            <aside>Sidebar ads</aside>
            <footer>Footer links</footer>
            </body></html>
            """;
        var httpClient = CreateHttpClient(html);
        var sut = new WebScraper(httpClient, CreateOptions(), logger);

        var result = await sut.ScrapeAsync(RecipeUrl.From("https://example.com/recipe"));

        result.IsSuccess.Should().BeTrue();
        result.Value.CleanedText.Should().NotContain("Menu items");
        result.Value.CleanedText.Should().NotContain("Site header");
        result.Value.CleanedText.Should().NotContain("Sidebar ads");
        result.Value.CleanedText.Should().NotContain("Footer links");
        result.Value.CleanedText.Should().Contain("Recipe content");
    }

    [Fact]
    public async Task ScrapeAsync_PrefersMainOverArticleOverBody()
    {
        var html = """
            <html><body>
            <p>Body text</p>
            <main><p>Main content</p></main>
            </body></html>
            """;
        var httpClient = CreateHttpClient(html);
        var sut = new WebScraper(httpClient, CreateOptions(), logger);

        var result = await sut.ScrapeAsync(RecipeUrl.From("https://example.com/recipe"));

        result.IsSuccess.Should().BeTrue();
        result.Value.CleanedText.Should().Contain("Main content");
    }

    [Fact]
    public async Task ScrapeAsync_FallsBackToArticleWhenNoMain()
    {
        var html = """
            <html><body>
            <p>Body text</p>
            <article><p>Article content</p></article>
            </body></html>
            """;
        var httpClient = CreateHttpClient(html);
        var sut = new WebScraper(httpClient, CreateOptions(), logger);

        var result = await sut.ScrapeAsync(RecipeUrl.From("https://example.com/recipe"));

        result.IsSuccess.Should().BeTrue();
        result.Value.CleanedText.Should().Contain("Article content");
    }

    [Fact]
    public async Task ScrapeAsync_FallsBackToBodyWhenNoMainOrArticle()
    {
        var html = "<html><body><p>Body only content</p></body></html>";
        var httpClient = CreateHttpClient(html);
        var sut = new WebScraper(httpClient, CreateOptions(), logger);

        var result = await sut.ScrapeAsync(RecipeUrl.From("https://example.com/recipe"));

        result.IsSuccess.Should().BeTrue();
        result.Value.CleanedText.Should().Contain("Body only content");
    }

    [Fact]
    public async Task ScrapeAsync_EmptyBody_ReturnsNoRecipeFound()
    {
        var html = "<html><body><script>only scripts</script></body></html>";
        var httpClient = CreateHttpClient(html);
        var sut = new WebScraper(httpClient, CreateOptions(), logger);

        var result = await sut.ScrapeAsync(RecipeUrl.From("https://example.com/recipe"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportRecipeErrors.NoRecipeFound);
    }

    [Fact]
    public async Task ScrapeAsync_HttpError_ReturnsPageUnreachable()
    {
        var httpClient = CreateHttpClient(statusCode: HttpStatusCode.InternalServerError);
        var sut = new WebScraper(httpClient, CreateOptions(), logger);

        var result = await sut.ScrapeAsync(RecipeUrl.From("https://example.com/recipe"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportRecipeErrors.PageUnreachable);
    }

    [Fact]
    public async Task ScrapeAsync_Timeout_ReturnsScrapeTimeout()
    {
        var handler = new TimeoutHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://example.com") };
        var sut = new WebScraper(httpClient, CreateOptions(), logger);

        var result = await sut.ScrapeAsync(RecipeUrl.From("https://example.com/recipe"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportRecipeErrors.ScrapeTimeout);
    }

    [Fact]
    public async Task ScrapeAsync_UserCancellation_ThrowsOperationCanceledException()
    {
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var httpClient = CreateHttpClient("<html><body>content</body></html>");
        var sut = new WebScraper(httpClient, CreateOptions(), logger);

        var act = () => sut.ScrapeAsync(
            RecipeUrl.From("https://example.com/recipe"), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ScrapeAsync_RemovesIframeAndNoscript()
    {
        var html = """
            <html><body>
            <iframe src="ad.html">Ad content</iframe>
            <noscript>Enable JavaScript</noscript>
            <main><p>Recipe text</p></main>
            </body></html>
            """;
        var httpClient = CreateHttpClient(html);
        var sut = new WebScraper(httpClient, CreateOptions(), logger);

        var result = await sut.ScrapeAsync(RecipeUrl.From("https://example.com/recipe"));

        result.IsSuccess.Should().BeTrue();
        result.Value.CleanedText.Should().NotContain("Ad content");
        result.Value.CleanedText.Should().NotContain("Enable JavaScript");
    }

    [Fact]
    public async Task ScrapeAsync_RawHtmlExceedsMaxLength_ReturnsContentTooLarge()
    {
        var html = new string('x', 1_000);
        var httpClient = CreateHttpClient(html);
        var sut = new WebScraper(httpClient, CreateOptions(maxRawHtmlLength: 500), logger);

        var result = await sut.ScrapeAsync(RecipeUrl.From("https://example.com/recipe"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportRecipeErrors.ContentTooLarge);
    }

    [Fact]
    public async Task ScrapeAsync_CleanedTextExceedsLimit_TruncatesContent()
    {
        var longText = string.Join(" ", Enumerable.Repeat("word", 500));
        var html = $"<html><body><main><p>{longText}</p></main></body></html>";
        var httpClient = CreateHttpClient(html);
        var sut = new WebScraper(httpClient, CreateOptions(maxContentLength: 100), logger);

        var result = await sut.ScrapeAsync(RecipeUrl.From("https://example.com/recipe"));

        result.IsSuccess.Should().BeTrue();
        result.Value.CleanedText.Length.Should().BeLessThanOrEqualTo(100);
    }

    [Fact]
    public async Task ScrapeAsync_CleanedTextWithinLimit_ReturnsFullContent()
    {
        var html = "<html><body><main><p>Short recipe content</p></main></body></html>";
        var httpClient = CreateHttpClient(html);
        var sut = new WebScraper(httpClient, CreateOptions(maxContentLength: 12_000), logger);

        var result = await sut.ScrapeAsync(RecipeUrl.From("https://example.com/recipe"));

        result.IsSuccess.Should().BeTrue();
        result.Value.CleanedText.Should().Contain("Short recipe content");
    }

    [Fact]
    public async Task ScrapeAsync_TruncationPreservesWordBoundary()
    {
        var html = "<html><body><main><p>hello world testing truncation here</p></main></body></html>";
        var httpClient = CreateHttpClient(html);
        var sut = new WebScraper(httpClient, CreateOptions(maxContentLength: 16), logger);

        var result = await sut.ScrapeAsync(RecipeUrl.From("https://example.com/recipe"));

        result.IsSuccess.Should().BeTrue();
        result.Value.CleanedText.Should().NotEndWith("t");
        result.Value.CleanedText.Should().EndWith("world");
    }

    private static HttpClient CreateHttpClient(string content = "", HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handler = new FakeHttpMessageHandler(content, statusCode);
        return new HttpClient(handler) { BaseAddress = new Uri("https://example.com") };
    }

    private sealed class FakeHttpMessageHandler(string content, HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content),
            };

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException("Request failed", null, statusCode);
            }

            return Task.FromResult(response);
        }
    }

    private sealed class TimeoutHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new TaskCanceledException("The request timed out.");
        }
    }
}
