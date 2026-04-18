using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;

public interface IWebScraper
{
	Task<Result<ScrapedContent>> ScrapeAsync(RecipeUrl url, CancellationToken cancellationToken = default);
}
