using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;

public interface IMealPlanReadModelRepository
{
	Task<WeeklyPlanDto> GetByOwnerAndWeekAsync(
		OwnerIdentifier owner,
		WeekIdentifier week,
		CancellationToken cancellationToken = default);

	Task<WeeklyPlannedRecipesDto> GetPlannedRecipesAsync(
		OwnerIdentifier owner,
		WeekIdentifier week,
		CancellationToken cancellationToken = default);

	Task<IReadOnlyList<MealHistoryEntryDto>> SearchCookedHistoryAsync(
		OwnerIdentifier owner,
		string? term,
		int limit,
		CancellationToken cancellationToken = default);
}
