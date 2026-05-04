using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Users.Application.DTOs;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;

namespace SmartSolutionsLab.Yumney.Users.Application.Queries.Handlers;

public sealed class GetSuggestionsQueryHandler(IUserActivityRepository activities, ICurrentUser currentUser)
	: IQueryHandler<GetSuggestionsQuery, Result<SuggestionsResponseDto>>
{
	public async Task<Result<SuggestionsResponseDto>> HandleAsync(GetSuggestionsQuery query, CancellationToken cancellationToken = default)
	{
		var owner = currentUser.AsOwner();
		var recentActivities = await activities.GetRecentAsync(owner, ActivityLimit.From(10), cancellationToken);
		var quickActions = BuildQuickActions(recentActivities);

		return Result.Success(new SuggestionsResponseDto(Suggestions: [], QuickActions: quickActions));
	}

	private static class MealHours
	{
		public const int BreakfastStart = 5;
		public const int BreakfastEnd = 11;
		public const int LunchEnd = 15;
		public const int DinnerEnd = 21;
	}

#pragma warning disable SA1303 // editorconfig requires camelCase for private const fields
	private const int recentImportLookbackHours = -1;
	private const int maxQuickActions = 4;
#pragma warning restore SA1303

	private static List<string> BuildQuickActions(IReadOnlyList<UserActivity> recentActivities)
	{
		var now = DateTime.UtcNow;
		var hour = now.Hour;
		List<string> actions = new(BuildTimeOfDayActions(hour));

		if (now.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
		{
			actions.Add("meal_prep");
			actions.Add("try_something_new");
		}

		var hasRecentImport = recentActivities.Any(activity =>
			activity.Type == ActivityType.RecipeImported &&
			activity.OccurredAt > now.AddHours(recentImportLookbackHours));

		if (hasRecentImport)
		{
			actions.Insert(0, "cook_now");
			actions.Insert(1, "add_to_shopping_list");
		}

		return actions.Distinct().Take(maxQuickActions).ToList();
	}

	private static IEnumerable<string> BuildTimeOfDayActions(int hour) => hour switch
	{
		>= MealHours.BreakfastStart and < MealHours.BreakfastEnd => ["breakfast_ideas", "quick_meals"],
		>= MealHours.BreakfastEnd and < MealHours.LunchEnd => ["lunch_recipes", "quick_meals"],
		>= MealHours.LunchEnd and < MealHours.DinnerEnd => ["whats_for_dinner", "30_min_recipes"],
		_ => ["snack_ideas", "meal_prep"],
	};
}
