using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Users.Application.DTOs;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;

namespace SmartSolutionsLab.Yumney.Users.Application.Queries.Handlers;

public sealed partial class GetSuggestionsQueryHandler(
    IUserActivityRepository activities,
    ICurrentUser currentUser,
    ILogger<GetSuggestionsQueryHandler> logger)
    : IQueryHandler<GetSuggestionsQuery, Result<SuggestionsResponseDto>>
{
    public async Task<Result<SuggestionsResponseDto>> HandleAsync(
        GetSuggestionsQuery query,
        CancellationToken cancellationToken = default)
    {
        var owner = OwnerIdentifier.From(currentUser.UserId);

        LogGetSuggestions(owner.Value);

        var recentActivities = await activities.GetRecentAsync(owner, 10, cancellationToken);

        var quickActions = BuildQuickActions(recentActivities);

        return Result.Success(new SuggestionsResponseDto(
            Suggestions: [],
            QuickActions: quickActions));
    }

    private static List<string> BuildQuickActions(IReadOnlyList<UserActivity> recentActivities)
    {
        var actions = new List<string>();
        var hour = DateTime.UtcNow.Hour;
        var dayOfWeek = DateTime.UtcNow.DayOfWeek;

        if (hour >= 5 && hour < 11)
        {
            actions.Add("breakfast_ideas");
            actions.Add("quick_meals");
        }
        else if (hour >= 11 && hour < 15)
        {
            actions.Add("lunch_recipes");
            actions.Add("quick_meals");
        }
        else if (hour >= 15 && hour < 21)
        {
            actions.Add("whats_for_dinner");
            actions.Add("30_min_recipes");
        }
        else
        {
            actions.Add("snack_ideas");
            actions.Add("meal_prep");
        }

        if (dayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            actions.Add("meal_prep");
            actions.Add("try_something_new");
        }

        var hasRecentImport = recentActivities.Any(a =>
            a.Type == ActivityType.RecipeImported &&
            a.OccurredAt > DateTime.UtcNow.AddHours(-1));

        if (hasRecentImport)
        {
            actions.Insert(0, "cook_now");
            actions.Insert(1, "add_to_shopping_list");
        }

        return actions.Distinct().Take(4).ToList();
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Getting suggestions for user {UserId}")]
    private partial void LogGetSuggestions(string userId);
}
