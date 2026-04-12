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
            var empty = WeeklyPlan.Create(owner, week);
            return new WeeklyPlanDto(week.Value, false, empty.GetVisibleSlots().ToOrderedDtos());
        }

        return new WeeklyPlanDto(week.Value, plan.IsExtendedMode, plan.GetVisibleSlots().ToOrderedDtos());
    }
}
