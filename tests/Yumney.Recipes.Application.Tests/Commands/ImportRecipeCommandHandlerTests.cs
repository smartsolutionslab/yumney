using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Tests.Commands;

public class ImportRecipeCommandHandlerTests
{
    private readonly IWebScraper scraper = Substitute.For<IWebScraper>();
    private readonly IRecipeExtractionService extraction = Substitute.For<IRecipeExtractionService>();
    private readonly ILogger<ImportRecipeCommandHandler> logger = Substitute.For<ILogger<ImportRecipeCommandHandler>>();

    [Fact]
    public async Task HandleAsync_ValidUrl_ReturnsExtractedRecipe()
    {
        var url = new RecipeUrl("https://example.com/recipe");
        var scrapedContent = new ScrapedContent("Recipe content", url);
        var expectedRecipe = CreateExtractedRecipe("Pasta Carbonara");

        scraper.ScrapeAsync(url, Arg.Any<CancellationToken>())
            .Returns(Result<ScrapedContent>.Success(scrapedContent));
        extraction.ExtractAsync(scrapedContent, Arg.Any<CancellationToken>())
            .Returns(Result<ExtractedRecipeDto>.Success(expectedRecipe));

        var sut = CreateSut();
        var result = await sut.HandleAsync(new ImportRecipeCommand(url));

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Pasta Carbonara");
    }

    [Fact]
    public async Task HandleAsync_ScrapeFailure_ReturnsFailure()
    {
        var url = new RecipeUrl("https://example.com/recipe");

        scraper.ScrapeAsync(url, Arg.Any<CancellationToken>())
            .Returns(Result<ScrapedContent>.Failure(ImportRecipeErrors.PageUnreachable));

        var sut = CreateSut();
        var result = await sut.HandleAsync(new ImportRecipeCommand(url));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportRecipeErrors.PageUnreachable);
    }

    [Fact]
    public async Task HandleAsync_NoRecipeFound_ReturnsFailure()
    {
        var url = new RecipeUrl("https://example.com/recipe");
        var scrapedContent = new ScrapedContent("Some content", url);

        scraper.ScrapeAsync(url, Arg.Any<CancellationToken>())
            .Returns(Result<ScrapedContent>.Success(scrapedContent));
        extraction.ExtractAsync(scrapedContent, Arg.Any<CancellationToken>())
            .Returns(Result<ExtractedRecipeDto>.Failure(ImportRecipeErrors.NoRecipeFound));

        var sut = CreateSut();
        var result = await sut.HandleAsync(new ImportRecipeCommand(url));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportRecipeErrors.NoRecipeFound);
    }

    [Fact]
    public async Task HandleAsync_ScrapeSucceeds_CallsExtraction()
    {
        var url = new RecipeUrl("https://example.com/recipe");
        var scrapedContent = new ScrapedContent("Recipe content", url);
        var expectedRecipe = CreateExtractedRecipe();

        scraper.ScrapeAsync(url, Arg.Any<CancellationToken>())
            .Returns(Result<ScrapedContent>.Success(scrapedContent));
        extraction.ExtractAsync(scrapedContent, Arg.Any<CancellationToken>())
            .Returns(Result<ExtractedRecipeDto>.Success(expectedRecipe));

        var sut = CreateSut();
        await sut.HandleAsync(new ImportRecipeCommand(url));

        await extraction.Received(1).ExtractAsync(scrapedContent, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ScrapeFailure_DoesNotCallExtraction()
    {
        var url = new RecipeUrl("https://example.com/recipe");

        scraper.ScrapeAsync(url, Arg.Any<CancellationToken>())
            .Returns(Result<ScrapedContent>.Failure(ImportRecipeErrors.PageUnreachable));

        var sut = CreateSut();
        await sut.HandleAsync(new ImportRecipeCommand(url));

        await extraction.DidNotReceive().ExtractAsync(
            Arg.Any<ScrapedContent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ScrapeTimeout_PropagatesTimeoutError()
    {
        var url = new RecipeUrl("https://example.com/recipe");

        scraper.ScrapeAsync(url, Arg.Any<CancellationToken>())
            .Returns(Result<ScrapedContent>.Failure(ImportRecipeErrors.ScrapeTimeout));

        var sut = CreateSut();
        var result = await sut.HandleAsync(new ImportRecipeCommand(url));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportRecipeErrors.ScrapeTimeout);
    }

    [Fact]
    public async Task HandleAsync_ExtractionFailed_PropagatesExtractionError()
    {
        var url = new RecipeUrl("https://example.com/recipe");
        var scrapedContent = new ScrapedContent("Some content", url);

        scraper.ScrapeAsync(url, Arg.Any<CancellationToken>())
            .Returns(Result<ScrapedContent>.Success(scrapedContent));
        extraction.ExtractAsync(scrapedContent, Arg.Any<CancellationToken>())
            .Returns(Result<ExtractedRecipeDto>.Failure(ImportRecipeErrors.ExtractionFailed));

        var sut = CreateSut();
        var result = await sut.HandleAsync(new ImportRecipeCommand(url));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportRecipeErrors.ExtractionFailed);
    }

    [Fact]
    public async Task HandleAsync_ValidUrl_PassesCancellationTokenToScraper()
    {
        var url = new RecipeUrl("https://example.com/recipe");
        var cts = new CancellationTokenSource();
        var scrapedContent = new ScrapedContent("Content", url);
        var expectedRecipe = CreateExtractedRecipe();

        scraper.ScrapeAsync(url, cts.Token)
            .Returns(Result<ScrapedContent>.Success(scrapedContent));
        extraction.ExtractAsync(scrapedContent, cts.Token)
            .Returns(Result<ExtractedRecipeDto>.Success(expectedRecipe));

        var sut = CreateSut();
        await sut.HandleAsync(new ImportRecipeCommand(url), cts.Token);

        await scraper.Received(1).ScrapeAsync(url, cts.Token);
    }

    [Fact]
    public async Task HandleAsync_ValidUrl_PassesCancellationTokenToExtraction()
    {
        var url = new RecipeUrl("https://example.com/recipe");
        var cts = new CancellationTokenSource();
        var scrapedContent = new ScrapedContent("Content", url);
        var expectedRecipe = CreateExtractedRecipe();

        scraper.ScrapeAsync(url, cts.Token)
            .Returns(Result<ScrapedContent>.Success(scrapedContent));
        extraction.ExtractAsync(scrapedContent, cts.Token)
            .Returns(Result<ExtractedRecipeDto>.Success(expectedRecipe));

        var sut = CreateSut();
        await sut.HandleAsync(new ImportRecipeCommand(url), cts.Token);

        await extraction.Received(1).ExtractAsync(scrapedContent, cts.Token);
    }

    [Fact]
    public async Task HandleAsync_MinimalRecipeData_ReturnsSuccess()
    {
        var url = new RecipeUrl("https://example.com/recipe");
        var scrapedContent = new ScrapedContent("Content", url);
        var minimalRecipe = new ExtractedRecipeDto("Simple Dish", [], []);

        scraper.ScrapeAsync(url, Arg.Any<CancellationToken>())
            .Returns(Result<ScrapedContent>.Success(scrapedContent));
        extraction.ExtractAsync(scrapedContent, Arg.Any<CancellationToken>())
            .Returns(Result<ExtractedRecipeDto>.Success(minimalRecipe));

        var sut = CreateSut();
        var result = await sut.HandleAsync(new ImportRecipeCommand(url));

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Simple Dish");
        result.Value.Description.Should().BeNull();
        result.Value.Servings.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_ContentTooLarge_ReturnsFailure()
    {
        var url = new RecipeUrl("https://example.com/recipe");

        scraper.ScrapeAsync(url, Arg.Any<CancellationToken>())
            .Returns(Result<ScrapedContent>.Failure(ImportRecipeErrors.ContentTooLarge));

        var sut = CreateSut();
        var result = await sut.HandleAsync(new ImportRecipeCommand(url));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportRecipeErrors.ContentTooLarge);
    }

    [Fact]
    public async Task HandleAsync_ScraperThrowsOperationCanceledException_PropagatesException()
    {
        var url = new RecipeUrl("https://example.com/recipe");
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        scraper.ScrapeAsync(url, cts.Token)
            .Returns<Result<ScrapedContent>>(_ => throw new OperationCanceledException(cts.Token));

        var sut = CreateSut();
        var act = () => sut.HandleAsync(new ImportRecipeCommand(url), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task HandleAsync_ExtractionThrowsOperationCanceledException_PropagatesException()
    {
        var url = new RecipeUrl("https://example.com/recipe");
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var scrapedContent = new ScrapedContent("Content", url);

        scraper.ScrapeAsync(url, cts.Token)
            .Returns(Result<ScrapedContent>.Success(scrapedContent));
        extraction.ExtractAsync(scrapedContent, cts.Token)
            .Returns<Result<ExtractedRecipeDto>>(_ => throw new OperationCanceledException(cts.Token));

        var sut = CreateSut();
        var act = () => sut.HandleAsync(new ImportRecipeCommand(url), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static ExtractedRecipeDto CreateExtractedRecipe(string title = "Test Recipe") =>
        new(title, [new ExtractedIngredientDto("Flour", 500, "g")], [new ExtractedStepDto(1, "Mix ingredients")], Servings: 4);

    private ImportRecipeCommandHandler CreateSut() => new(scraper, extraction, logger);
}
