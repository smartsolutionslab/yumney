using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using Yumney.Recipes.Application.Commands;
using Yumney.Recipes.Domain.Recipe;
using Yumney.Recipes.Infrastructure.Services;

namespace Yumney.Recipes.Infrastructure.Tests.Services;

public class WebScraperTests
{
    private readonly ILogger<WebScraper> logger = Substitute.For<ILogger<WebScraper>>();

    [Fact]
    public async Task ScrapeAsync_ValidHtml_ReturnsCleanedText()
    {
        var html = "<html><body><main><h1>Recipe</h1><p>Delicious pasta</p></main></body></html>";
        var httpClient = CreateHttpClient(html);
        var sut = new WebScraper(httpClient, logger);

        var result = await sut.ScrapeAsync(new RecipeUrl("https://example.com/recipe"));

        result.IsSuccess.Should().BeTrue();
        result.Value.CleanedText.Should().Contain("Recipe");
        result.Value.CleanedText.Should().Contain("Delicious pasta");
        result.Value.SourceUrl.Should().Be("https://example.com/recipe");
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
        var sut = new WebScraper(httpClient, logger);

        var result = await sut.ScrapeAsync(new RecipeUrl("https://example.com/recipe"));

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
        var sut = new WebScraper(httpClient, logger);

        var result = await sut.ScrapeAsync(new RecipeUrl("https://example.com/recipe"));

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
        var sut = new WebScraper(httpClient, logger);

        var result = await sut.ScrapeAsync(new RecipeUrl("https://example.com/recipe"));

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
        var sut = new WebScraper(httpClient, logger);

        var result = await sut.ScrapeAsync(new RecipeUrl("https://example.com/recipe"));

        result.IsSuccess.Should().BeTrue();
        result.Value.CleanedText.Should().Contain("Article content");
    }

    [Fact]
    public async Task ScrapeAsync_FallsBackToBodyWhenNoMainOrArticle()
    {
        var html = "<html><body><p>Body only content</p></body></html>";
        var httpClient = CreateHttpClient(html);
        var sut = new WebScraper(httpClient, logger);

        var result = await sut.ScrapeAsync(new RecipeUrl("https://example.com/recipe"));

        result.IsSuccess.Should().BeTrue();
        result.Value.CleanedText.Should().Contain("Body only content");
    }

    [Fact]
    public async Task ScrapeAsync_EmptyBody_ReturnsNoRecipeFound()
    {
        var html = "<html><body><script>only scripts</script></body></html>";
        var httpClient = CreateHttpClient(html);
        var sut = new WebScraper(httpClient, logger);

        var result = await sut.ScrapeAsync(new RecipeUrl("https://example.com/recipe"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportRecipeErrors.NoRecipeFound);
    }

    [Fact]
    public async Task ScrapeAsync_HttpError_ReturnsPageUnreachable()
    {
        var httpClient = CreateHttpClient(statusCode: HttpStatusCode.InternalServerError);
        var sut = new WebScraper(httpClient, logger);

        var result = await sut.ScrapeAsync(new RecipeUrl("https://example.com/recipe"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportRecipeErrors.PageUnreachable);
    }

    [Fact]
    public async Task ScrapeAsync_Timeout_ReturnsScrapeTimeout()
    {
        var handler = new TimeoutHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://example.com") };
        var sut = new WebScraper(httpClient, logger);

        var result = await sut.ScrapeAsync(new RecipeUrl("https://example.com/recipe"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportRecipeErrors.ScrapeTimeout);
    }

    [Fact]
    public async Task ScrapeAsync_UserCancellation_ThrowsOperationCanceledException()
    {
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var httpClient = CreateHttpClient("<html><body>content</body></html>");
        var sut = new WebScraper(httpClient, logger);

        var act = () => sut.ScrapeAsync(
            new RecipeUrl("https://example.com/recipe"), cts.Token);

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
        var sut = new WebScraper(httpClient, logger);

        var result = await sut.ScrapeAsync(new RecipeUrl("https://example.com/recipe"));

        result.IsSuccess.Should().BeTrue();
        result.Value.CleanedText.Should().NotContain("Ad content");
        result.Value.CleanedText.Should().NotContain("Enable JavaScript");
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
