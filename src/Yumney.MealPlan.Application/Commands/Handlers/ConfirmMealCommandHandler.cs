using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;

public sealed class ConfirmMealCommandHandler(
    IWeeklyPlanRepository plans,
    ICurrentUser currentUser) : ICommandHandler<ConfirmMealCommand, Result<WeeklyPlanDto>>
{
    /// <inheritdoc />
    public async Task<Result<WeeklyPlanDto>> HandleAsync(ConfirmMealCommand command, CancellationToken cancellationToken = default)
    {
        var owner = currentUser.AsOwner();
        var week = WeekIdentifier.From(command.Year, command.WeekNumber);

        var plan = await plans.GetByOwnerAndWeekAsync(owner, week, cancellationToken);

        switch (command.NewState)
        {
            case MealState.Cooked:
                plan.MarkAsCooked(command.Day, command.MealType);
                break;
            case MealState.Skipped:
                plan.MarkAsSkipped(command.Day, command.MealType);
                break;
            case MealState.Planned:
                plan.ResetToPlanned(command.Day, command.MealType);
                break;
        }

        await plans.SaveChangesAsync(cancellationToken);

        return new WeeklyPlanDto(week.Value, plan.IsExtendedMode, plan.GetVisibleSlots().ToOrderedDtos());
    }
}
