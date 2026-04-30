using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;
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

	[Fact]
	public async Task HandleAsync_ValidUrl_ReturnsExtractedRecipe()
	{
		var url = RecipeUrl.From("https://example.com/recipe");
		var scrapedContent = new ScrapedContent("Recipe content", url);
		var expectedRecipe = CreateExtractedRecipe("Pasta Carbonara");

		scraper.ScrapeAsync(url, Arg.Any<CancellationToken>())
			.Returns(Result<ScrapedContent>.Success(scrapedContent));
		extraction.ExtractAsync(scrapedContent, Arg.Any<CancellationToken>())
			.Returns(Result<ExtractedRecipeDto>.Success(expectedRecipe));

		var handler = CreateHandler();
		var result = await handler.HandleAsync(new ImportRecipeCommand(url));

		result.IsSuccess.Should().BeTrue();
		result.Value.Title.Should().Be("Pasta Carbonara");
	}

	[Fact]
	public async Task HandleAsync_ScrapeFailure_ReturnsFailure()
	{
		var url = RecipeUrl.From("https://example.com/recipe");

		scraper.ScrapeAsync(url, Arg.Any<CancellationToken>())
			.Returns(Result<ScrapedContent>.Failure(ImportRecipeErrors.PageUnreachable));

		var handler = CreateHandler();
		var result = await handler.HandleAsync(new ImportRecipeCommand(url));

		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(ImportRecipeErrors.PageUnreachable);
	}

	[Fact]
	public async Task HandleAsync_NoRecipeFound_ReturnsFailure()
	{
		var url = RecipeUrl.From("https://example.com/recipe");
		var scrapedContent = new ScrapedContent("Some content", url);

		scraper.ScrapeAsync(url, Arg.Any<CancellationToken>())
			.Returns(Result<ScrapedContent>.Success(scrapedContent));
		extraction.ExtractAsync(scrapedContent, Arg.Any<CancellationToken>())
			.Returns(Result<ExtractedRecipeDto>.Failure(ImportRecipeErrors.NoRecipeFound));

		var handler = CreateHandler();
		var result = await handler.HandleAsync(new ImportRecipeCommand(url));

		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(ImportRecipeErrors.NoRecipeFound);
	}

	[Fact]
	public async Task HandleAsync_ScrapeSucceeds_CallsExtraction()
	{
		var url = RecipeUrl.From("https://example.com/recipe");
		var scrapedContent = new ScrapedContent("Recipe content", url);
		var expectedRecipe = CreateExtractedRecipe();

		scraper.ScrapeAsync(url, Arg.Any<CancellationToken>())
			.Returns(Result<ScrapedContent>.Success(scrapedContent));
		extraction.ExtractAsync(scrapedContent, Arg.Any<CancellationToken>())
			.Returns(Result<ExtractedRecipeDto>.Success(expectedRecipe));

		var handler = CreateHandler();
		await handler.HandleAsync(new ImportRecipeCommand(url));

		await extraction.Received(1).ExtractAsync(scrapedContent, Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_ScrapeFailure_DoesNotCallExtraction()
	{
		var url = RecipeUrl.From("https://example.com/recipe");

		scraper.ScrapeAsync(url, Arg.Any<CancellationToken>())
			.Returns(Result<ScrapedContent>.Failure(ImportRecipeErrors.PageUnreachable));

		var handler = CreateHandler();
		await handler.HandleAsync(new ImportRecipeCommand(url));

		await extraction.DidNotReceive().ExtractAsync(
			Arg.Any<ScrapedContent>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_ScrapeTimeout_PropagatesTimeoutError()
	{
		var url = RecipeUrl.From("https://example.com/recipe");

		scraper.ScrapeAsync(url, Arg.Any<CancellationToken>())
			.Returns(Result<ScrapedContent>.Failure(ImportRecipeErrors.ScrapeTimeout));

		var handler = CreateHandler();
		var result = await handler.HandleAsync(new ImportRecipeCommand(url));

		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(ImportRecipeErrors.ScrapeTimeout);
	}

	[Fact]
	public async Task HandleAsync_ExtractionFailed_PropagatesExtractionError()
	{
		var url = RecipeUrl.From("https://example.com/recipe");
		var scrapedContent = new ScrapedContent("Some content", url);

		scraper.ScrapeAsync(url, Arg.Any<CancellationToken>())
			.Returns(Result<ScrapedContent>.Success(scrapedContent));
		extraction.ExtractAsync(scrapedContent, Arg.Any<CancellationToken>())
			.Returns(Result<ExtractedRecipeDto>.Failure(ImportRecipeErrors.ExtractionFailed));

		var handler = CreateHandler();
		var result = await handler.HandleAsync(new ImportRecipeCommand(url));

		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(ImportRecipeErrors.ExtractionFailed);
	}

	[Fact]
	public async Task HandleAsync_ValidUrl_PassesCancellationTokenToScraper()
	{
		var url = RecipeUrl.From("https://example.com/recipe");
		var cts = new CancellationTokenSource();
		var scrapedContent = new ScrapedContent("Content", url);
		var expectedRecipe = CreateExtractedRecipe();

		scraper.ScrapeAsync(url, cts.Token)
			.Returns(Result<ScrapedContent>.Success(scrapedContent));
		extraction.ExtractAsync(scrapedContent, cts.Token)
			.Returns(Result<ExtractedRecipeDto>.Success(expectedRecipe));

		var handler = CreateHandler();
		await handler.HandleAsync(new ImportRecipeCommand(url), cts.Token);

		await scraper.Received(1).ScrapeAsync(url, cts.Token);
	}

	[Fact]
	public async Task HandleAsync_ValidUrl_PassesCancellationTokenToExtraction()
	{
		var url = RecipeUrl.From("https://example.com/recipe");
		var cts = new CancellationTokenSource();
		var scrapedContent = new ScrapedContent("Content", url);
		var expectedRecipe = CreateExtractedRecipe();

		scraper.ScrapeAsync(url, cts.Token)
			.Returns(Result<ScrapedContent>.Success(scrapedContent));
		extraction.ExtractAsync(scrapedContent, cts.Token)
			.Returns(Result<ExtractedRecipeDto>.Success(expectedRecipe));

		var handler = CreateHandler();
		await handler.HandleAsync(new ImportRecipeCommand(url), cts.Token);

		await extraction.Received(1).ExtractAsync(scrapedContent, cts.Token);
	}

	[Fact]
	public async Task HandleAsync_MinimalRecipeData_ReturnsSuccess()
	{
		var url = RecipeUrl.From("https://example.com/recipe");
		var scrapedContent = new ScrapedContent("Content", url);
		var minimalRecipe = new ExtractedRecipeDto("Simple Dish", [], []);

		scraper.ScrapeAsync(url, Arg.Any<CancellationToken>())
			.Returns(Result<ScrapedContent>.Success(scrapedContent));
		extraction.ExtractAsync(scrapedContent, Arg.Any<CancellationToken>())
			.Returns(Result<ExtractedRecipeDto>.Success(minimalRecipe));

		var handler = CreateHandler();
		var result = await handler.HandleAsync(new ImportRecipeCommand(url));

		result.IsSuccess.Should().BeTrue();
		result.Value.Title.Should().Be("Simple Dish");
		result.Value.Description.Should().BeNull();
		result.Value.Servings.Should().BeNull();
	}

	[Fact]
	public async Task HandleAsync_ContentTooLarge_ReturnsFailure()
	{
		var url = RecipeUrl.From("https://example.com/recipe");

		scraper.ScrapeAsync(url, Arg.Any<CancellationToken>())
			.Returns(Result<ScrapedContent>.Failure(ImportRecipeErrors.ContentTooLarge));

		var handler = CreateHandler();
		var result = await handler.HandleAsync(new ImportRecipeCommand(url));

		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(ImportRecipeErrors.ContentTooLarge);
	}

	[Fact]
	public async Task HandleAsync_ScraperThrowsOperationCanceledException_PropagatesException()
	{
		var url = RecipeUrl.From("https://example.com/recipe");
		var cts = new CancellationTokenSource();
		await cts.CancelAsync();

		scraper.ScrapeAsync(url, cts.Token)
			.Returns<Result<ScrapedContent>>(_ => throw new OperationCanceledException(cts.Token));

		var handler = CreateHandler();
		var act = () => handler.HandleAsync(new ImportRecipeCommand(url), cts.Token);

		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	[Fact]
	public async Task HandleAsync_ExtractionThrowsOperationCanceledException_PropagatesException()
	{
		var url = RecipeUrl.From("https://example.com/recipe");
		var cts = new CancellationTokenSource();
		await cts.CancelAsync();

		var scrapedContent = new ScrapedContent("Content", url);

		scraper.ScrapeAsync(url, cts.Token)
			.Returns(Result<ScrapedContent>.Success(scrapedContent));
		extraction.ExtractAsync(scrapedContent, cts.Token)
			.Returns<Result<ExtractedRecipeDto>>(_ => throw new OperationCanceledException(cts.Token));

		var handler = CreateHandler();
		var act = () => handler.HandleAsync(new ImportRecipeCommand(url), cts.Token);

		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	private static ExtractedRecipeDto CreateExtractedRecipe(string title = "Test Recipe") =>
		new(title, [new ExtractedIngredientDto("Flour", 500, "g")], [new ExtractedStepDto(1, "Mix ingredients")], Servings: 4);

	private ImportRecipeCommandHandler CreateHandler() => new(scraper, extraction);
}
