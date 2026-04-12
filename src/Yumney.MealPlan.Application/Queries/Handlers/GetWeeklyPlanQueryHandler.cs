using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Queries.Handlers;

public sealed class GetWeeklyPlanQueryHandler(
    IWeeklyPlanRepository plans,
    ICurrentUser currentUser) : IQueryHandler<GetWeeklyPlanQuery, Result<WeeklyPlanDto>>
{
    public async Task<Result<WeeklyPlanDto>> HandleAsync(GetWeeklyPlanQuery query, CancellationToken cancellationToken = default)
    {
        var owner = currentUser.AsOwner();
        var week = WeekIdentifier.From(query.Year, query.WeekNumber);

        var plan = await plans.FindByOwnerAndWeekAsync(owner, week, cancellationToken);

        if (plan is null)
        {
            var emptySlots = Enumerable.Range(0, 7)
                .Select(i => new MealSlotDto(
                    ((DayOfWeek)((i + 1) % 7)).ToString(),
                    MealType.Dinner.ToString(),
                    null,
                    null,
                    4,
                    true))
                .ToList();
            return new WeeklyPlanDto(week.Value, false, emptySlots);
        }

        var visibleSlots = plan.GetVisibleSlots()
            .OrderBy(s => s.Day)
            .ThenBy(s => s.MealType)
            .Select(s => new MealSlotDto(s.Day.ToString(), s.MealType.ToString(), s.RecipeIdentifier, s.RecipeTitle, s.Servings, s.IsEmpty))
            .ToList();

        return new WeeklyPlanDto(week.Value, plan.IsExtendedMode, visibleSlots);
    }
}
