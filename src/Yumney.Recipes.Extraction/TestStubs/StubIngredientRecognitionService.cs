using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.TestStubs;

internal sealed class StubIngredientRecognitionService : IIngredientRecognitionService
{
	public Task<Result<RecognizedIngredientsResponseDto>> RecognizeAsync(PhotoData photo, CancellationToken cancellationToken = default)
	{
		IReadOnlyList<RecognizedIngredientDto> recognized =
		[
			new RecognizedIngredientDto("tomato", 0.95, "produce"),
			new RecognizedIngredientDto("onion", 0.90, "produce"),
		];
		return Task.FromResult(Result<RecognizedIngredientsResponseDto>.Success(new RecognizedIngredientsResponseDto(recognized)));
	}
}
