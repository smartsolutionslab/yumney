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
		var owner = currentUser.AsOwner();

		var entries = query.Type is null
			? await activities.GetRecentByCursorAsync(owner, query.Limit, query.Cursor, cancellationToken)
			: await activities.GetRecentByTypeAndCursorAsync(owner, query.Type, query.Limit, query.Cursor, cancellationToken);

		var nextCursor = entries.Count == query.Limit.Value
			? ActivityCursor.From(entries[^1].OccurredAt, entries[^1].Id).Encode()
			: null;

		return Result.Success(new UserActivityPageDto(entries.ToDtos(), nextCursor));
	}
}
