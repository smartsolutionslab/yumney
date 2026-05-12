using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.MealPlan.Extraction.TestStubs;

/// <summary>
/// Deterministic fake used in E2E + integration tests. Spreads the first 7 catalog entries
/// across Monday-Sunday so flows can assert against a known shape without hitting the LLM.
/// </summary>
#pragma warning disable SA1311
public sealed class StubWeekSuggestionService : IWeekSuggestionService
{
	private static readonly string[] days = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];

	public Task<Result<IReadOnlyList<WeekSuggestionEntryDto>>> SuggestAsync(
		WeekIdentifier week,
		IReadOnlyList<RecipeCatalogEntry> catalog,
		IReadOnlyList<MealHistoryEntryDto> recentHistory,
		DietaryProfileSnapshot dietary,
		CancellationToken cancellationToken = default)
	{
		if (catalog.Count == 0)
		{
			return Task.FromResult(Result<IReadOnlyList<WeekSuggestionEntryDto>>.Success([]));
		}

		var entries = days
			.Select((day, index) => new WeekSuggestionEntryDto(
				day,
				"Dinner",
				catalog[index % catalog.Count].RecipeIdentifier,
				catalog[index % catalog.Count].Title,
				"Never cooked",
				"Stub suggestion"))
			.ToList();

		return Task.FromResult(Result<IReadOnlyList<WeekSuggestionEntryDto>>.Success(entries));
	}
}
