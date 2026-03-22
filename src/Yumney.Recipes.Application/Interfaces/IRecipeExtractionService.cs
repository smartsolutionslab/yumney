using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;

public interface IRecipeExtractionService
{
    Task<Result<ExtractedRecipeDto>> ExtractAsync(ScrapedContent content, CancellationToken cancellationToken = default);

    Task<Result<ExtractedRecipeDto>> ExtractFromPhotosAsync(IReadOnlyList<PhotoData> photos, CancellationToken cancellationToken = default);
}
