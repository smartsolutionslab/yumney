using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;

public sealed class ToggleExtendedModeCommandHandler(
    IWeeklyPlanRepository plans,
    ICurrentUser currentUser) : ICommandHandler<ToggleExtendedModeCommand, Result<WeeklyPlanDto>>
{
    public async Task<Result<WeeklyPlanDto>> HandleAsync(ToggleExtendedModeCommand command, CancellationToken cancellationToken = default)
    {
        var owner = currentUser.AsOwner();
        var week = WeekIdentifier.From(command.Year, command.WeekNumber);

        var plan = await plans.FindByOwnerAndWeekAsync(owner, week, cancellationToken);
        if (plan is null)
        {
            plan = WeeklyPlan.Create(owner, week);
            if (command.Enable)
                plan.EnableExtendedMode();
            await plans.AddAsync(plan, cancellationToken);
        }
        else
        {
            plan = await plans.GetByOwnerAndWeekAsync(owner, week, cancellationToken);
            if (command.Enable)
                plan.EnableExtendedMode();
            else
                plan.DisableExtendedMode();
            await plans.SaveChangesAsync(cancellationToken);
        }

        return new WeeklyPlanDto(week.Value, plan.IsExtendedMode, plan.GetVisibleSlots().ToOrderedDtos());
    }
}
