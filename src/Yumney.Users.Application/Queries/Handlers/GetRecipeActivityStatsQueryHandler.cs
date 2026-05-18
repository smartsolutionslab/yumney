using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Users.Application.DTOs;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;

namespace SmartSolutionsLab.Yumney.Users.Application.Queries.Handlers;

public sealed class GetRecipeActivityStatsQueryHandler(IUserActivityRepository activities, ICurrentUser currentUser)
	: IQueryHandler<GetRecipeActivityStatsQuery, Result<RecipeActivityStatsDto>>
{
	public async Task<Result<RecipeActivityStatsDto>> HandleAsync(GetRecipeActivityStatsQuery query, CancellationToken cancellationToken = default)
	{
		var recipe = RecipeIdentifierSnapshot.From(query.RecipeIdentifier);
		var owner = currentUser.AsOwner();

		var stats = await activities.GetRecipeStatsAsync(owner, recipe, cancellationToken);
		return Result.Success(stats.ToDto());
	}
}
