using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Tests.Commands;

public class ImportRecipeFromTextCommandHandlerTests
{
	private readonly IRecipeExtractionService extraction = Substitute.For<IRecipeExtractionService>();
	private readonly ImportRecipeFromTextCommandHandler handler;

	public ImportRecipeFromTextCommandHandlerTests()
	{
		handler = new ImportRecipeFromTextCommandHandler(extraction);
	}

	[Fact]
	public async Task HandleAsync_ValidText_ReturnsExtractedRecipe()
	{
		var expected = new ExtractedRecipeDto("Pasta Carbonara", [], [], Description: "Classic Italian");
		extraction.ExtractAsync(Arg.Any<ScrapedContent>(), Arg.Any<CancellationToken>())
			.Returns(Result<ExtractedRecipeDto>.Success(expected));

		var command = new ImportRecipeFromTextCommand("Pasta Carbonara\n200g spaghetti\nMix with eggs and cheese");

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();
		result.Value.Title.Should().Be("Pasta Carbonara");
	}

	[Fact]
	public async Task HandleAsync_ExtractionFails_ReturnsFailure()
	{
		extraction.ExtractAsync(Arg.Any<ScrapedContent>(), Arg.Any<CancellationToken>())
			.Returns(ImportRecipeErrors.ExtractionFailed);

		var command = new ImportRecipeFromTextCommand("Not a recipe");

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeFalse();
		result.Error!.Code.Should().Be("IMPORT_EXTRACTION_FAILED");
	}

	[Fact]
	public async Task HandleAsync_PassesTextAsScrapedContent()
	{
		var expected = new ExtractedRecipeDto("Test", [], []);
		extraction.ExtractAsync(Arg.Any<ScrapedContent>(), Arg.Any<CancellationToken>())
			.Returns(Result<ExtractedRecipeDto>.Success(expected));

		var recipeText = "My recipe\nIngredients: flour, eggs";
		var command = new ImportRecipeFromTextCommand(recipeText);

		await handler.HandleAsync(command);

		await extraction.Received(1).ExtractAsync(
			Arg.Is<ScrapedContent>(c => c.CleanedText == recipeText && c.SourceUrl == null),
			Arg.Any<CancellationToken>());
	}
}
