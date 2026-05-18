using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Users.Application.DTOs;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;

namespace SmartSolutionsLab.Yumney.Users.Application.Queries.Handlers;

public sealed class GetRecentActivityQueryHandler(IUserActivityRepository activities, ICurrentUser currentUser)
	: IQueryHandler<GetRecentActivityQuery, Result<UserActivityPageDto>>
{
	public async Task<Result<UserActivityPageDto>> HandleAsync(GetRecentActivityQuery query, CancellationToken cancellationToken = default)
	{
		var (limit, type, cursor) = query;
		var owner = currentUser.AsOwner();

		var entries = type is null
			? await activities.GetRecentByCursorAsync(owner, limit, cursor, cancellationToken)
			: await activities.GetRecentByTypeAndCursorAsync(owner, type, limit, cursor, cancellationToken);

		var nextCursor = entries.Count == limit.Value
			? ActivityCursor.From(entries[^1].OccurredAt, entries[^1].Id).Encode()
			: null;

		return Result.Success(new UserActivityPageDto(entries.ToDtos(), nextCursor));
	}
}
