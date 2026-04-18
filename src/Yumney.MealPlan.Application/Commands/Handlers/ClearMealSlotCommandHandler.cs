using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;

public sealed class ClearMealSlotCommandHandler(IWeeklyPlanRepository plans, ICurrentUser currentUser)
	: ICommandHandler<ClearMealSlotCommand, Result<WeeklyPlanDto>>
{
    public async Task<Result<WeeklyPlanDto>> HandleAsync(ClearMealSlotCommand command, CancellationToken cancellationToken = default)
    {
        var (week, day, mealType) = command;
        var owner = currentUser.AsOwner();

        var plan = await plans.GetByOwnerAndWeekAsync(owner, week, cancellationToken);

        plan.ClearSlot(day, mealType);

        await plans.SaveChangesAsync(cancellationToken);

        return new WeeklyPlanDto(week.Value, plan.IsExtendedMode, plan.GetVisibleSlots().ToOrderedDtos());
    }
}
