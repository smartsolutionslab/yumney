using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Tests.Commands;

public class ImportRecipeFromPhotosCommandHandlerTests
{
    private readonly IRecipeExtractionService extraction = Substitute.For<IRecipeExtractionService>();
    private readonly ILogger<ImportRecipeFromPhotosCommandHandler> logger =
        Substitute.For<ILogger<ImportRecipeFromPhotosCommandHandler>>();

    [Fact]
    public async Task HandleAsync_ValidPhoto_ReturnsExtractedRecipe()
    {
        var photos = new[] { CreatePhoto() };
        var expected = CreateExtractedRecipe();

        extraction.ExtractFromPhotosAsync(Arg.Any<IReadOnlyList<PhotoData>>(), Arg.Any<CancellationToken>())
            .Returns(Result<ExtractedRecipeDto>.Success(expected));

        var result = await CreateSut().HandleAsync(new ImportRecipeFromPhotosCommand(photos));

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

        await CreateSut().HandleAsync(new ImportRecipeFromPhotosCommand(photos));

        capturedPhotos.Should().HaveCount(2);
    }

    [Fact]
    public async Task HandleAsync_EmptyPhotos_ReturnsFailure()
    {
        var result = await CreateSut().HandleAsync(new ImportRecipeFromPhotosCommand([]));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportRecipeErrors.TooManyPhotos);
    }

    [Fact]
    public async Task HandleAsync_TooManyPhotos_ReturnsFailure()
    {
        var photos = Enumerable.Range(0, 11).Select(i => CreatePhoto($"photo{i}.jpg")).ToList();

        var result = await CreateSut().HandleAsync(new ImportRecipeFromPhotosCommand(photos));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportRecipeErrors.TooManyPhotos);
    }

    [Fact]
    public async Task HandleAsync_PhotoTooLarge_ReturnsFailure()
    {
        var largePhoto = new PhotoData(new byte[11 * 1024 * 1024], "image/jpeg", "large.jpg");

        var result = await CreateSut().HandleAsync(new ImportRecipeFromPhotosCommand([largePhoto]));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportRecipeErrors.PhotoTooLarge);
    }

    [Fact]
    public async Task HandleAsync_InvalidFormat_ReturnsFailure()
    {
        var pdfFile = new PhotoData(new byte[100], "application/pdf", "recipe.pdf");

        var result = await CreateSut().HandleAsync(new ImportRecipeFromPhotosCommand([pdfFile]));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportRecipeErrors.InvalidPhotoFormat);
    }

    [Fact]
    public async Task HandleAsync_ExtractionFails_ReturnsFailure()
    {
        var photos = new[] { CreatePhoto() };

        extraction.ExtractFromPhotosAsync(Arg.Any<IReadOnlyList<PhotoData>>(), Arg.Any<CancellationToken>())
            .Returns(Result<ExtractedRecipeDto>.Failure(ImportRecipeErrors.ExtractionFailed));

        var result = await CreateSut().HandleAsync(new ImportRecipeFromPhotosCommand(photos));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportRecipeErrors.ExtractionFailed);
    }

    [Fact]
    public async Task HandleAsync_TenPhotos_Succeeds()
    {
        var photos = Enumerable.Range(0, 10).Select(i => CreatePhoto($"photo{i}.jpg")).ToList();

        extraction.ExtractFromPhotosAsync(Arg.Any<IReadOnlyList<PhotoData>>(), Arg.Any<CancellationToken>())
            .Returns(Result<ExtractedRecipeDto>.Success(CreateExtractedRecipe()));

        var result = await CreateSut().HandleAsync(new ImportRecipeFromPhotosCommand(photos));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_WebpPhoto_Succeeds()
    {
        var webpPhoto = new PhotoData(new byte[100], "image/webp", "recipe.webp");

        extraction.ExtractFromPhotosAsync(Arg.Any<IReadOnlyList<PhotoData>>(), Arg.Any<CancellationToken>())
            .Returns(Result<ExtractedRecipeDto>.Success(CreateExtractedRecipe()));

        var result = await CreateSut().HandleAsync(new ImportRecipeFromPhotosCommand([webpPhoto]));

        result.IsSuccess.Should().BeTrue();
    }

    private static PhotoData CreatePhoto(string fileName = "recipe.jpg") =>
        new(new byte[1024], "image/jpeg", fileName);

    private static ExtractedRecipeDto CreateExtractedRecipe() =>
        new("Test Recipe", [new ExtractedIngredientDto("Flour", 500, "g")], [new ExtractedStepDto(1, "Mix")], Servings: 4);

    private ImportRecipeFromPhotosCommandHandler CreateSut() => new(extraction, logger);
}
