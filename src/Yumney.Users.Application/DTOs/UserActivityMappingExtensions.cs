using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;

namespace SmartSolutionsLab.Yumney.Users.Application.DTOs;

public static class UserActivityMappingExtensions
{
	public static UserActivityDto ToDto(this UserActivity activity) =>
		new(
			activity.Type.Value,
			activity.RecipeIdentifier?.Value,
			activity.RecipeTitle?.Value,
			activity.OccurredAt);

	public static IReadOnlyList<UserActivityDto> ToDtos(this IEnumerable<UserActivity> activities) =>
		activities.Select(activity => activity.ToDto()).ToList();

	public static RecipeActivityStatsDto ToDto(this RecipeActivityStats stats) =>
		new(stats.CookCount, stats.LastCookedAt, stats.ViewCount);
}
