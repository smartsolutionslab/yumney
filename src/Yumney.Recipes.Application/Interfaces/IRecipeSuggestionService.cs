using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;

/// <summary>
/// LLM-backed recipe suggestion service for US-343. Takes a snapshot of the
/// user's available ingredients + dietary constraints and returns a fixed
/// number of generated recipes shaped like <see cref="ExtractedRecipeDto"/>
/// so the existing save flow can persist them unchanged.
/// </summary>
public interface IRecipeSuggestionService
{
	Task<Result<IReadOnlyList<ExtractedRecipeDto>>> SuggestAsync(
		IReadOnlyCollection<string> availableIngredients,
		string? dietaryType,
		IReadOnlyCollection<string> restrictions,
		int count,
		CancellationToken cancellationToken = default);
}
