using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shared.Paging;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Queries.Handlers;

public sealed class SearchMealHistoryQueryHandler(IMealPlanReadModelRepository readModel, ICurrentUser currentUser)
	: IQueryHandler<SearchMealHistoryQuery, Result<PagedResult<MealHistoryEntryDto>>>
{
	public async Task<Result<PagedResult<MealHistoryEntryDto>>> HandleAsync(
		SearchMealHistoryQuery query,
		CancellationToken cancellationToken = default)
	{
		var (paging, term) = query;

		var owner = currentUser.AsOwner();
		var page = await readModel.SearchCookedHistoryAsync(owner, term, paging, cancellationToken);
		return Result<PagedResult<MealHistoryEntryDto>>.Success(page);
	}
}
