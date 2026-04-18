using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Users.Application.DTOs;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;

namespace SmartSolutionsLab.Yumney.Users.Application.Queries.Handlers;

public sealed class GetRecentActivityQueryHandler(
	IUserActivityRepository activities,
	ICurrentUser currentUser)
	: IQueryHandler<GetRecentActivityQuery, Result<IReadOnlyList<UserActivityDto>>>
{
	public async Task<Result<IReadOnlyList<UserActivityDto>>> HandleAsync(GetRecentActivityQuery query, CancellationToken cancellationToken = default)
	{
		var owner = currentUser.AsOwner();

		var recentActivities = await activities.GetRecentAsync(owner, query.Limit, cancellationToken);

		var dtos = recentActivities.Select(a => a.ToDto()).ToList();

		return Result.Success<IReadOnlyList<UserActivityDto>>(dtos);
	}
}
