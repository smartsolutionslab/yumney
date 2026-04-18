using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;

public sealed class ImportRecipeFromTextCommandHandler(
	IRecipeExtractionService extraction)
	: ICommandHandler<ImportRecipeFromTextCommand, Result<ExtractedRecipeDto>>
{
	public async Task<Result<ExtractedRecipeDto>> HandleAsync(ImportRecipeFromTextCommand command, CancellationToken cancellationToken = default)
	{
		var text = command.RecipeText;

		var content = new ScrapedContent(text, SourceUrl: string.Empty);
		var extractResult = await extraction.ExtractAsync(content, cancellationToken);

		if (extractResult.IsFailure)
		{
			return extractResult.Error!;
		}

		return extractResult;
	}
}
