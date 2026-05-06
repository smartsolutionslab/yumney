using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Paging;

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

	Task<PagedResult<MealHistoryEntryDto>> SearchCookedHistoryAsync(
		OwnerIdentifier owner,
		SearchTerm? term,
		PagingOptions paging,
		CancellationToken cancellationToken = default);
}
