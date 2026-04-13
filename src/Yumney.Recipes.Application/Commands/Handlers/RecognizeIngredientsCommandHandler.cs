using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;

public sealed class RecognizeIngredientsCommandHandler(
    IIngredientRecognitionService recognitionService)
    : ICommandHandler<RecognizeIngredientsCommand, Result<RecognizedIngredientsResponseDto>>
{
    public async Task<Result<RecognizedIngredientsResponseDto>> HandleAsync(
        RecognizeIngredientsCommand command,
        CancellationToken cancellationToken = default)
    {
        return await recognitionService.RecognizeAsync(command.Photo, cancellationToken);
    }
}
