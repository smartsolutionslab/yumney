using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Users.Application.DTOs;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;

namespace SmartSolutionsLab.Yumney.Users.Application.Queries.Handlers;

public sealed class GetRecipeActivityStatsQueryHandler(
	IUserActivityRepository activities,
	ICurrentUser currentUser)
	: IQueryHandler<GetRecipeActivityStatsQuery, Result<RecipeActivityStatsDto>>
{
	public async Task<Result<RecipeActivityStatsDto>> HandleAsync(GetRecipeActivityStatsQuery query, CancellationToken cancellationToken = default)
	{
		var owner = currentUser.AsOwner();
		var stats = await activities.GetRecipeStatsAsync(
			owner,
			RecipeIdentifierSnapshot.From(query.RecipeIdentifier),
			cancellationToken);
		return Result.Success(new RecipeActivityStatsDto(stats.CookCount, stats.LastCookedAt, stats.ViewCount));
	}
}
