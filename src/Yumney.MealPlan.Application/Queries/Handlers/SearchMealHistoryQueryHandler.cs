using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Queries.Handlers;

public sealed class SearchMealHistoryQueryHandler(IMealPlanReadModelRepository readModel, ICurrentUser currentUser)
	: IQueryHandler<SearchMealHistoryQuery, Result<IReadOnlyList<MealHistoryEntryDto>>>
{
#pragma warning disable SA1303
	private const int minLimit = 1;
	private const int maxLimit = 100;
#pragma warning restore SA1303

	public async Task<Result<IReadOnlyList<MealHistoryEntryDto>>> HandleAsync(
		SearchMealHistoryQuery query,
		CancellationToken cancellationToken = default)
	{
		var owner = currentUser.AsOwner();
		var limit = Math.Clamp(query.Limit, minLimit, maxLimit);

		var result = await readModel.SearchCookedHistoryAsync(owner, query.Term, limit, cancellationToken);
		return Result<IReadOnlyList<MealHistoryEntryDto>>.Success(result);
	}
}
