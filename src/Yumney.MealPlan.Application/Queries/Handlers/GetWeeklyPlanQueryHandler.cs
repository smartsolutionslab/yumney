using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Queries.Handlers;

public sealed class GetWeeklyPlanQueryHandler(IMealPlanReadModelRepository readModel, ICurrentUser currentUser)
	: IQueryHandler<GetWeeklyPlanQuery, Result<WeeklyPlanDto>>
{
	public async Task<Result<WeeklyPlanDto>> HandleAsync(GetWeeklyPlanQuery query, CancellationToken cancellationToken = default)
	{
		var owner = currentUser.AsOwner();
		return await readModel.GetByOwnerAndWeekAsync(owner, query.Week, cancellationToken);
	}
}
