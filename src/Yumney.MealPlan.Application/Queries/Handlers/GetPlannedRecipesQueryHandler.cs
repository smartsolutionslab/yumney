using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Queries.Handlers;

/// <summary>
/// Returns all recipes planned for a week (Recipe slots only).
/// Leftover and Freetext slots are excluded — they don't contribute to shopping.
/// </summary>
public sealed class GetPlannedRecipesQueryHandler(
    IWeeklyPlanRepository plans,
    ICurrentUser currentUser) : IQueryHandler<GetPlannedRecipesQuery, Result<WeeklyPlannedRecipesDto>>
{
    /// <inheritdoc />
    public async Task<Result<WeeklyPlannedRecipesDto>> HandleAsync(GetPlannedRecipesQuery query, CancellationToken cancellationToken = default)
    {
        var owner = currentUser.AsOwner();
        var week = WeekIdentifier.From(query.Year, query.WeekNumber);

        var plan = await plans.FindByOwnerAndWeekAsync(owner, week, cancellationToken);
        if (plan is null)
            return new WeeklyPlannedRecipesDto(week.Value, []);

        var recipes = plan.Slots
            .Where(s => s.ContentType == SlotContentType.Recipe && s.RecipeIdentifier.HasValue)
            .Select(s => new PlannedRecipeDto(
                s.RecipeIdentifier!.Value,
                s.RecipeTitle!,
                s.Servings,
                s.Day.ToString(),
                s.MealType.ToString()))
            .ToList();

        return new WeeklyPlannedRecipesDto(week.Value, recipes);
    }
}
