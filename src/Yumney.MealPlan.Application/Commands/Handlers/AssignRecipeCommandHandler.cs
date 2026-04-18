using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;

public sealed class AssignRecipeCommandHandler(
    IWeeklyPlanRepository plans,
    ICurrentUser currentUser) : ICommandHandler<AssignRecipeCommand, Result<WeeklyPlanDto>>
{
    public async Task<Result<WeeklyPlanDto>> HandleAsync(AssignRecipeCommand command, CancellationToken cancellationToken = default)
    {
        var (week, day, recipe, mealType, servings) = command;
        var owner = currentUser.AsOwner();

        var plan = await plans.FindByOwnerAndWeekAsync(owner, week, cancellationToken);
        if (plan is null)
        {
            plan = WeeklyPlan.Create(owner, week);
            plan.AssignRecipe(day, recipe, mealType, servings);
            await plans.AddAsync(plan, cancellationToken);
        }
        else
        {
            plan = await plans.GetByOwnerAndWeekAsync(owner, week, cancellationToken);
            plan.AssignRecipe(day, recipe, mealType, servings);
            await plans.SaveChangesAsync(cancellationToken);
        }

        return new WeeklyPlanDto(week.Value, plan.IsExtendedMode, plan.GetVisibleSlots().ToOrderedDtos());
    }
}
