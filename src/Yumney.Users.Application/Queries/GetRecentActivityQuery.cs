using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Users.Application.DTOs;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;

namespace SmartSolutionsLab.Yumney.Users.Application.Queries;

/// <summary>
/// Returns the current user's most recent activity entries (US-121, US-576).
/// When <see cref="Type"/> is non-null only entries of that type are included.
/// When <see cref="Cursor"/> is non-null returns entries strictly older than
/// the cursor (keyset pagination).
/// </summary>
public sealed record GetRecentActivityQuery(
	ActivityLimit Limit,
	ActivityType? Type = null,
	ActivityCursor? Cursor = null)
	: IQuery<Result<UserActivityPageDto>>;
