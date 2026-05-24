using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Tests.Commands;

public class ImportRecipeCommandHandlerTests
{
	private readonly IWebScraper scraper = Substitute.For<IWebScraper>();
	private readonly IRecipeExtractionService extraction = Substitute.For<IRecipeExtractionService>();
	private readonly ICommandHandler<SaveRecipeCommand, Result<SavedRecipeDto>> saveHandler = Substitute.For<ICommandHandler<SaveRecipeCommand, Result<SavedRecipeDto>>>();

	public ImportRecipeCommandHandlerTests()
	{
		// Default: save succeeds. Individual tests can override.
		saveHandler.HandleAsync(Arg.Any<SaveRecipeCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result<SavedRecipeDto>.Success(new SavedRecipeDto(Guid.NewGuid(), "Pasta Carbonara", DateTime.UtcNow)));
	}

	[Fact]
	public async Task HandleAsync_ValidUrl_ReturnsSavedRecipe()
	{
		var url = RecipeUrl.From("https://example.com/recipe");
		var scrapedContent = new ScrapedContent("Recipe content", url);
		var extracted = CreateExtractedRecipe("Pasta Carbonara");

		scraper.ScrapeAsync(url, Arg.Any<CancellationToken>())
			.Returns(Result<ScrapedContent>.Success(scrapedContent));
		extraction.ExtractAsync(scrapedContent, Arg.Any<CancellationToken>())
			.Returns(Result<ExtractedRecipeDto>.Success(extracted));

		var handler = CreateHandler();
		var result = await handler.HandleAsync(new ImportRecipeCommand(url));

		result.IsSuccess.Should().BeTrue();
		result.Value.Title.Should().Be("Pasta Carbonara");
		result.Value.Identifier.Should().NotBe(Guid.Empty);
	}

	[Fact]
	public async Task HandleAsync_ValidUrl_InvokesSaveHandlerWithExtractedFields()
	{
		// Regression test for #820. The handler used to return ExtractedRecipeDto
		// without persisting — MCP / Voice clients then saw "IsError=False" but
		// nothing landed in the user's collection.
		var url = RecipeUrl.From("https://example.com/banana-bread");
		var scrapedContent = new ScrapedContent("banana bread content", url);
		var extracted = new ExtractedRecipeDto(
			"Banana Bread",
			[new ExtractedIngredientDto("Banana", 3, null), new ExtractedIngredientDto("Flour", 250, "g")],
			[new ExtractedStepDto(1, "Mash bananas"), new ExtractedStepDto(2, "Mix and bake")],
			Description: "Classic banana bread",
			Servings: 8,
			PrepTimeMinutes: 15,
			CookTimeMinutes: 60,
			ImageUrl: "https://example.com/banana.jpg");

		scraper.ScrapeAsync(url, Arg.Any<CancellationToken>())
			.Returns(Result<ScrapedContent>.Success(scrapedContent));
		extraction.ExtractAsync(scrapedContent, Arg.Any<CancellationToken>())
			.Returns(Result<ExtractedRecipeDto>.Success(extracted));

		SaveRecipeCommand? captured = null;
		saveHandler
			.HandleAsync(Arg.Do<SaveRecipeCommand>(command => captured = command), Arg.Any<CancellationToken>())
			.Returns(Result<SavedRecipeDto>.Success(new SavedRecipeDto(Guid.NewGuid(), "Banana Bread", DateTime.UtcNow)));

		var handler = CreateHandler();
		await handler.HandleAsync(new ImportRecipeCommand(url));

		await saveHandler.Received(1).HandleAsync(Arg.Any<SaveRecipeCommand>(), Arg.Any<CancellationToken>());
		captured.Should().NotBeNull();
		captured!.Title.Value.Should().Be("Banana Bread");
		captured.Ingredients.Should().HaveCount(2);
		captured.Steps.Should().HaveCount(2);
		captured.Servings!.Value.Should().Be(8);
		captured.Timing!.Preparation!.Value.Should().Be(15);
		captured.Timing.Cooking!.Value.Should().Be(60);
		captured.SourceUrl.Should().Be(url);
	}

	[Fact]
	public async Task HandleAsync_ScrapeFailure_DoesNotInvokeSave()
	{
		var url = RecipeUrl.From("https://example.com/recipe");

		scraper.ScrapeAsync(url, Arg.Any<CancellationToken>())
			.Returns(Result<ScrapedContent>.Failure(ImportRecipeErrors.PageUnreachable));

		var handler = CreateHandler();
		var result = await handler.HandleAsync(new ImportRecipeCommand(url));

		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(ImportRecipeErrors.PageUnreachable);
		await saveHandler.DidNotReceive().HandleAsync(Arg.Any<SaveRecipeCommand>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_ExtractionFailure_DoesNotInvokeSave()
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
		await saveHandler.DidNotReceive().HandleAsync(Arg.Any<SaveRecipeCommand>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_SaveFailure_PropagatesError()
	{
		var url = RecipeUrl.From("https://example.com/recipe");
		var scrapedContent = new ScrapedContent("Content", url);

		scraper.ScrapeAsync(url, Arg.Any<CancellationToken>())
			.Returns(Result<ScrapedContent>.Success(scrapedContent));
		extraction.ExtractAsync(scrapedContent, Arg.Any<CancellationToken>())
			.Returns(Result<ExtractedRecipeDto>.Success(CreateExtractedRecipe()));
		saveHandler.HandleAsync(Arg.Any<SaveRecipeCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result<SavedRecipeDto>.Failure(SaveRecipeErrors.AlreadyImported));

		var handler = CreateHandler();
		var result = await handler.HandleAsync(new ImportRecipeCommand(url));

		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(SaveRecipeErrors.AlreadyImported);
	}

	[Fact]
	public async Task HandleAsync_ScrapeFailure_DoesNotCallExtraction()
	{
		var url = RecipeUrl.From("https://example.com/recipe");

		scraper.ScrapeAsync(url, Arg.Any<CancellationToken>())
			.Returns(Result<ScrapedContent>.Failure(ImportRecipeErrors.PageUnreachable));

		var handler = CreateHandler();
		await handler.HandleAsync(new ImportRecipeCommand(url));

		await extraction.DidNotReceive().ExtractAsync(Arg.Any<ScrapedContent>(), Arg.Any<CancellationToken>());
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
	public async Task HandleAsync_ValidUrl_PassesCancellationTokenToScraper()
	{
		var url = RecipeUrl.From("https://example.com/recipe");
		var cts = new CancellationTokenSource();
		var scrapedContent = new ScrapedContent("Content", url);

		scraper.ScrapeAsync(url, cts.Token)
			.Returns(Result<ScrapedContent>.Success(scrapedContent));
		extraction.ExtractAsync(scrapedContent, cts.Token)
			.Returns(Result<ExtractedRecipeDto>.Success(CreateExtractedRecipe()));

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

		scraper.ScrapeAsync(url, cts.Token)
			.Returns(Result<ScrapedContent>.Success(scrapedContent));
		extraction.ExtractAsync(scrapedContent, cts.Token)
			.Returns(Result<ExtractedRecipeDto>.Success(CreateExtractedRecipe()));

		var handler = CreateHandler();
		await handler.HandleAsync(new ImportRecipeCommand(url), cts.Token);

		await extraction.Received(1).ExtractAsync(scrapedContent, cts.Token);
	}

	[Fact]
	public async Task HandleAsync_ValidUrl_PassesCancellationTokenToSave()
	{
		var url = RecipeUrl.From("https://example.com/recipe");
		var cts = new CancellationTokenSource();
		var scrapedContent = new ScrapedContent("Content", url);

		scraper.ScrapeAsync(url, cts.Token)
			.Returns(Result<ScrapedContent>.Success(scrapedContent));
		extraction.ExtractAsync(scrapedContent, cts.Token)
			.Returns(Result<ExtractedRecipeDto>.Success(CreateExtractedRecipe()));

		var handler = CreateHandler();
		await handler.HandleAsync(new ImportRecipeCommand(url), cts.Token);

		await saveHandler.Received(1).HandleAsync(Arg.Any<SaveRecipeCommand>(), cts.Token);
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

	private ImportRecipeCommandHandler CreateHandler() => new(scraper, extraction, saveHandler);
}
