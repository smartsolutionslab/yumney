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
        Ensure.That(command.TotalServings).IsPositive();
        Ensure.That(command.EatServings).IsPositive();
        Ensure.That(command.RecipeTitle).IsNotNullOrWhiteSpace();

        var owner = currentUser.AsOwner();
        var week = WeekIdentifier.From(command.Year, command.WeekNumber);
        var leftoverServings = command.TotalServings - command.EatServings;

        var plan = await plans.FindByOwnerAndWeekAsync(owner, week, cancellationToken);
        if (plan is null)
        {
            plan = WeeklyPlan.Create(owner, week);
            plan.AssignRecipe(command.CookDay, command.RecipeIdentifier, command.RecipeTitle, command.MealType, command.TotalServings);
            if (leftoverServings > 0)
                plan.SetLeftover(command.LeftoverDay, command.CookDay, command.MealType, command.RecipeTitle, command.MealType, leftoverServings);
            await plans.AddAsync(plan, cancellationToken);
        }
        else
        {
            plan = await plans.GetByOwnerAndWeekAsync(owner, week, cancellationToken);
            plan.AssignRecipe(command.CookDay, command.RecipeIdentifier, command.RecipeTitle, command.MealType, command.TotalServings);
            if (leftoverServings > 0)
                plan.SetLeftover(command.LeftoverDay, command.CookDay, command.MealType, command.RecipeTitle, command.MealType, leftoverServings);
            await plans.SaveChangesAsync(cancellationToken);
        }

        return new WeeklyPlanDto(week.Value, plan.IsExtendedMode, plan.GetVisibleSlots().ToOrderedDtos());
    }
}
