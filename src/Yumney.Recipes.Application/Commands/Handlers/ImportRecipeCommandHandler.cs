using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;

// MCP / Voice clients call this via POST /api/v1/recipes/import — there's no
// user-review step in the LLM flow, so the handler extracts AND persists.
// Earlier shape (extract-only, returning ExtractedRecipeDto) was a #820 bug:
// the capability description promised "add it to the user's collection" but
// the implementation just parsed and threw the result away. The UI uses the
// SSE-streaming endpoint for the review-and-save pattern; this command is
// specifically the headless import-and-save path.
public sealed class ImportRecipeCommandHandler(
	IWebScraper scraper,
	IRecipeExtractionService extraction,
	ICommandHandler<SaveRecipeCommand, Result<SavedRecipeDto>> saveHandler)
	: ICommandHandler<ImportRecipeCommand, Result<SavedRecipeDto>>
{
	public async Task<Result<SavedRecipeDto>> HandleAsync(ImportRecipeCommand command, CancellationToken cancellationToken = default)
	{
		var url = command.Url;

		var scrapeResult = await scraper.ScrapeAsync(url, cancellationToken);
		if (scrapeResult.IsFailure) return scrapeResult.Error!;

		var extractResult = await extraction.ExtractAsync(scrapeResult.Value, cancellationToken);
		if (extractResult.IsFailure) return extractResult.Error!;

		var saveCommand = BuildSaveCommand(extractResult.Value, url);
		return await saveHandler.HandleAsync(saveCommand, cancellationToken);
	}

	private static SaveRecipeCommand BuildSaveCommand(ExtractedRecipeDto extracted, RecipeUrl sourceUrl)
	{
		var ingredients = extracted.Ingredients
			.Select(item => new SaveRecipeIngredientItem(
				IngredientName.From(item.Name),
				Quantity.FromNullable(
					Amount.FromNullable(item.Amount),
					Unit.FromNullable(item.Unit))))
			.ToList();

		var steps = extracted.Steps
			.Select(step => new SaveRecipeStepItem(StepNumber.From(step.Number), StepDescription.From(step.Description)))
			.ToList();

		return new SaveRecipeCommand(
			RecipeTitle.From(extracted.Title),
			ingredients,
			steps,
			RecipeDescription.FromNullable(extracted.Description),
			Domain.Recipe.Servings.FromNullable(extracted.Servings),
			TimingInfo.FromNullable(
				PreparationTime.FromNullable(extracted.PrepTimeMinutes),
				CookingTime.FromNullable(extracted.CookTimeMinutes)),
			Domain.Recipe.Difficulty.FromNullable(extracted.Difficulty),
			Domain.Recipe.ImageUrl.FromNullable(extracted.ImageUrl),
			RecipeLanguage.FromNullable(extracted.Language),
			sourceUrl);
	}
}
