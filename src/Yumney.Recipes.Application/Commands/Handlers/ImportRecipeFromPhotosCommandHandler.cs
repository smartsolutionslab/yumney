using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;

public sealed class ImportRecipeFromPhotosCommandHandler(IRecipeExtractionService extractionService)
	: ICommandHandler<ImportRecipeFromPhotosCommand, Result<ExtractedRecipeDto>>
{
	public async Task<Result<ExtractedRecipeDto>> HandleAsync(ImportRecipeFromPhotosCommand command, CancellationToken cancellationToken = default)
	{
		var photos = command.Photos;

		return await extractionService.ExtractFromPhotosAsync(photos, cancellationToken);
	}
}
