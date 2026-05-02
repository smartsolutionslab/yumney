using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.TestStubs;

internal sealed class StubWebScraper : IWebScraper
{
	public Task<Result<ScrapedContent>> ScrapeAsync(RecipeUrl url, CancellationToken cancellationToken = default) =>
		Task.FromResult(Result<ScrapedContent>.Success(new ScrapedContent(
			CleanedText: "Stub scraped page content for E2E tests.",
			SourceUrl: url,
			StructuredRecipe: null)));
}
