using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Tests.Commands;

public class RecognizeIngredientsCommandHandlerTests
{
	private readonly IIngredientRecognitionService recognitionService = Substitute.For<IIngredientRecognitionService>();
	private readonly RecognizeIngredientsCommandHandler handler;

	public RecognizeIngredientsCommandHandlerTests()
	{
		handler = new RecognizeIngredientsCommandHandler(recognitionService);
	}

	[Fact]
	public async Task HandleAsync_DelegatesToRecognitionService()
	{
		var photo = new PhotoData([1, 2, 3], "image/jpeg", "photo.jpg");
		var expected = new RecognizedIngredientsResponseDto([new RecognizedIngredientDto("Tomato", 0.9, "produce")]);

		recognitionService.RecognizeAsync(photo, Arg.Any<CancellationToken>())
			.Returns(Result<RecognizedIngredientsResponseDto>.Success(expected));

		var result = await handler.HandleAsync(new RecognizeIngredientsCommand(photo));

		result.IsSuccess.Should().BeTrue();
		result.Value.Ingredients.Should().HaveCount(1);
		result.Value.Ingredients[0].Name.Should().Be("Tomato");
	}

	[Fact]
	public async Task HandleAsync_ServiceReturnsFailure_PropagatesFailure()
	{
		var photo = new PhotoData([1], "image/png", "test.png");
		var error = new ApiError("RECOGNITION_FAILED", "Failed to recognize", 500);

		recognitionService.RecognizeAsync(photo, Arg.Any<CancellationToken>())
			.Returns(Result<RecognizedIngredientsResponseDto>.Failure(error));

		var result = await handler.HandleAsync(new RecognizeIngredientsCommand(photo));

		result.IsSuccess.Should().BeFalse();
		result.Error!.Code.Should().Be("RECOGNITION_FAILED");
	}

	[Fact]
	public async Task HandleAsync_PassesPhotoToService()
	{
		var photo = new PhotoData([10, 20, 30, 40], "image/webp", "snap.webp");

		recognitionService.RecognizeAsync(
				Arg.Any<PhotoData>(),
				Arg.Any<CancellationToken>())
			.Returns(Result<RecognizedIngredientsResponseDto>.Success(new RecognizedIngredientsResponseDto([])));

		await handler.HandleAsync(new RecognizeIngredientsCommand(photo));

		await recognitionService.Received(1).RecognizeAsync(photo, Arg.Any<CancellationToken>());
	}
}
