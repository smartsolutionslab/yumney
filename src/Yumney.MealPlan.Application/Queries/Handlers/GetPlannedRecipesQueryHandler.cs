using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Queries.Handlers;

public sealed class GetPlannedRecipesQueryHandler(IMealPlanReadModelRepository readModel, ICurrentUser currentUser)
	: IQueryHandler<GetPlannedRecipesQuery, Result<WeeklyPlannedRecipesDto>>
{
	public async Task<Result<WeeklyPlannedRecipesDto>> HandleAsync(
		GetPlannedRecipesQuery query,
		CancellationToken cancellationToken = default)
	{
		var owner = currentUser.AsOwner();
		return await readModel.GetPlannedRecipesAsync(owner, query.Week, cancellationToken);
	}
}
