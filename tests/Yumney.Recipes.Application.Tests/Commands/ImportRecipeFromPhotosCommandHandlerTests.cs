using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Tests.Commands;

public class ImportRecipeFromPhotosCommandHandlerTests
{
	private readonly IRecipeExtractionService extraction = Substitute.For<IRecipeExtractionService>();

	[Fact]
	public async Task HandleAsync_ValidPhoto_ReturnsExtractedRecipe()
	{
		var photos = new[] { CreatePhoto() };
		var expected = CreateExtractedRecipe();

		extraction.ExtractFromPhotosAsync(Arg.Any<IReadOnlyList<PhotoData>>(), Arg.Any<CancellationToken>())
			.Returns(Result<ExtractedRecipeDto>.Success(expected));

		var result = await CreateHandler().HandleAsync(new ImportRecipeFromPhotosCommand(photos));

		result.IsSuccess.Should().BeTrue();
		result.Value.Title.Should().Be("Test Recipe");
	}

	[Fact]
	public async Task HandleAsync_MultiplePhotos_PassesAllToExtractionService()
	{
		var photos = new[] { CreatePhoto("page1.jpg"), CreatePhoto("page2.jpg") };
		IReadOnlyList<PhotoData>? capturedPhotos = null;

		extraction.ExtractFromPhotosAsync(Arg.Any<IReadOnlyList<PhotoData>>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				capturedPhotos = callInfo.ArgAt<IReadOnlyList<PhotoData>>(0);
				return Result<ExtractedRecipeDto>.Success(CreateExtractedRecipe());
			});

		await CreateHandler().HandleAsync(new ImportRecipeFromPhotosCommand(photos));

		capturedPhotos.Should().HaveCount(2);
	}

	[Fact]
	public async Task HandleAsync_ExtractionFails_ReturnsFailure()
	{
		var photos = new[] { CreatePhoto() };

		extraction.ExtractFromPhotosAsync(Arg.Any<IReadOnlyList<PhotoData>>(), Arg.Any<CancellationToken>())
			.Returns(Result<ExtractedRecipeDto>.Failure(ImportRecipeErrors.ExtractionFailed));

		var result = await CreateHandler().HandleAsync(new ImportRecipeFromPhotosCommand(photos));

		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(ImportRecipeErrors.ExtractionFailed);
	}

	private static PhotoData CreatePhoto(string fileName = "recipe.jpg") =>
		new(new byte[1024], "image/jpeg", fileName);

	private static ExtractedRecipeDto CreateExtractedRecipe() =>
		new("Test Recipe", [new ExtractedIngredientDto("Flour", 500, "g")], [new ExtractedStepDto(1, "Mix")], Servings: 4);

	private ImportRecipeFromPhotosCommandHandler CreateHandler() => new(extraction);
}
