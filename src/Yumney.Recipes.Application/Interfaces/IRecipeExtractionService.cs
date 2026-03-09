using Yumney.Recipes.Application.DTOs;
using Yumney.Shared.Common;

namespace Yumney.Recipes.Application.Interfaces;

public interface IRecipeExtractionService
{
    Task<Result<ExtractedRecipeDto>> ExtractAsync(ScrapedContent content, CancellationToken cancellationToken = default);
}
