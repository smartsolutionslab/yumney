using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;

public interface IWeekSuggestionService
{
	Task<Result<IReadOnlyList<WeekSuggestionEntryDto>>> SuggestAsync(
		WeekIdentifier week,
		IReadOnlyList<RecipeCatalogEntry> catalog,
		IReadOnlyList<MealHistoryEntryDto> recentHistory,
		DietaryProfileSnapshot dietary,
		CancellationToken cancellationToken = default);
}
