using System.Runtime.CompilerServices;
using System.Text.Json;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.TestStubs;

internal sealed class StubRecipeExtractionService : IRecipeExtractionService
{
	public Task<Result<ExtractedRecipeDto>> ExtractAsync(ScrapedContent content, CancellationToken cancellationToken = default) =>
		Task.FromResult(Result<ExtractedRecipeDto>.Success(StubRecipes.Sample()));

	public Task<Result<ExtractedRecipeDto>> ExtractFromPhotosAsync(IReadOnlyList<PhotoData> photos, CancellationToken cancellationToken = default) =>
		Task.FromResult(Result<ExtractedRecipeDto>.Success(StubRecipes.Sample()));

	public async IAsyncEnumerable<string> StreamExtractAsync(ScrapedContent content, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		yield return JsonSerializer.Serialize(StubRecipes.Sample());
		await Task.CompletedTask;
	}
}
