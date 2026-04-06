using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Users.Application.DTOs;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;

namespace SmartSolutionsLab.Yumney.Users.Application.Queries.Handlers;

#pragma warning disable SA1601
public sealed partial class GetRecentActivityQueryHandler(
#pragma warning restore SA1601
    IUserActivityRepository activities,
    ICurrentUser currentUser,
    ILogger<GetRecentActivityQueryHandler> logger)
    : IQueryHandler<GetRecentActivityQuery, Result<IReadOnlyList<UserActivityDto>>>
{
    public async Task<Result<IReadOnlyList<UserActivityDto>>> HandleAsync(GetRecentActivityQuery query, CancellationToken cancellationToken = default)
    {
        var owner = OwnerIdentifier.From(currentUser.UserId);

        LogGetRecentActivity(owner.Value, query.Limit);

        var recentActivities = await activities.GetRecentAsync(owner, query.Limit, cancellationToken);

        var dtos = recentActivities.Select(a => a.ToDto()).ToList();

        return Result.Success<IReadOnlyList<UserActivityDto>>(dtos);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Getting recent activity for user {UserId}, limit {Limit}")]
    private partial void LogGetRecentActivity(string userId, int limit);
}
