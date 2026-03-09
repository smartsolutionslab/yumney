using Yumney.Recipes.Application.DTOs;
using Yumney.Recipes.Domain.Recipe;
using Yumney.Shared.Common;

namespace Yumney.Recipes.Application.Interfaces;

public interface IWebScraper
{
    Task<Result<ScrapedContent>> ScrapeAsync(RecipeUrl url, CancellationToken cancellationToken = default);
}
