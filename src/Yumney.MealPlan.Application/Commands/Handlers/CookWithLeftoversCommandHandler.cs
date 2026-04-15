using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;

public sealed class CookWithLeftoversCommandHandler(
    IWeeklyPlanRepository plans,
    ICurrentUser currentUser) : ICommandHandler<CookWithLeftoversCommand, Result<WeeklyPlanDto>>
{
    public async Task<Result<WeeklyPlanDto>> HandleAsync(CookWithLeftoversCommand command, CancellationToken cancellationToken = default)
    {
        var (year, weekNumber, cookDay, recipeIdentifier, recipeTitle, totalServings, eatServings, leftoverDay, mealType) = command;
        Ensure.That(totalServings).IsPositive();
        Ensure.That(eatServings).IsPositive();
        Ensure.That(recipeTitle).IsNotNullOrWhiteSpace();

        var owner = currentUser.AsOwner();
        var week = WeekIdentifier.From(year, weekNumber);
        var leftoverServings = totalServings - eatServings;

        var plan = await plans.FindByOwnerAndWeekAsync(owner, week, cancellationToken);
        if (plan is null)
        {
            plan = WeeklyPlan.Create(owner, week);
            plan.AssignRecipe(cookDay, recipeIdentifier, recipeTitle, mealType, totalServings);
            if (leftoverServings > 0)
                plan.SetLeftover(leftoverDay, cookDay, mealType, recipeTitle, mealType, leftoverServings);
            await plans.AddAsync(plan, cancellationToken);
        }
        else
        {
            plan = await plans.GetByOwnerAndWeekAsync(owner, week, cancellationToken);
            plan.AssignRecipe(cookDay, recipeIdentifier, recipeTitle, mealType, totalServings);
            if (leftoverServings > 0)
                plan.SetLeftover(leftoverDay, cookDay, mealType, recipeTitle, mealType, leftoverServings);
            await plans.SaveChangesAsync(cancellationToken);
        }

        return new WeeklyPlanDto(week.Value, plan.IsExtendedMode, plan.GetVisibleSlots().ToOrderedDtos());
    }
}
