using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;

public interface IIngredientRecognitionService
{
    Task<Result<RecognizedIngredientsResponseDto>> RecognizeAsync(
        PhotoData photo,
        CancellationToken cancellationToken = default);
}
