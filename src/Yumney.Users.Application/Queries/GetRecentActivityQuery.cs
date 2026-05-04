using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Users.Application.DTOs;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;

namespace SmartSolutionsLab.Yumney.Users.Application.Queries;

/// <summary>
/// Returns the current user's most recent activity entries (US-121). When
/// <see cref="Type"/> is non-null only entries of that type are included.
/// </summary>
public sealed record GetRecentActivityQuery(ActivityLimit Limit, ActivityType? Type = null)
	: IQuery<Result<IReadOnlyList<UserActivityDto>>>;
