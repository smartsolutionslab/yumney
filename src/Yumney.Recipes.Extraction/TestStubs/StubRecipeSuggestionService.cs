using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.TestStubs;

internal sealed class StubRecipeSuggestionService : IRecipeSuggestionService
{
	public Task<Result<IReadOnlyList<ExtractedRecipeDto>>> SuggestAsync(
		IReadOnlyCollection<string> availableIngredients,
		string? dietaryType,
		IReadOnlyCollection<string> restrictions,
		int count,
		CancellationToken cancellationToken = default)
	{
		IReadOnlyList<ExtractedRecipeDto> suggestions =
			Enumerable.Range(1, Math.Max(1, count))
				.Select(index => StubRecipes.Sample($"#{index}"))
				.ToList();

		return Task.FromResult(Result<IReadOnlyList<ExtractedRecipeDto>>.Success(suggestions));
	}
}
