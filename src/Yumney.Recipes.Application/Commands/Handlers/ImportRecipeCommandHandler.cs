using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;

public sealed class ImportRecipeCommandHandler(IWebScraper scraper, IRecipeExtractionService extraction)
	: ICommandHandler<ImportRecipeCommand, Result<ExtractedRecipeDto>>
{
	public async Task<Result<ExtractedRecipeDto>> HandleAsync(ImportRecipeCommand command, CancellationToken cancellationToken = default)
	{
		var url = command.Url;

		var scrapeResult = await scraper.ScrapeAsync(url, cancellationToken);

		if (scrapeResult.IsFailure)
		{
			return scrapeResult.Error!;
		}

		var extractResult = await extraction.ExtractAsync(scrapeResult.Value, cancellationToken);
		if (extractResult.IsFailure)
		{
			return extractResult.Error!;
		}

		return extractResult;
	}
}
