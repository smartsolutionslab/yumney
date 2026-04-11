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
        var owner = currentUser.AsOwner();
        var week = WeekIdentifier.From(command.Year, command.WeekNumber);

        var plan = await plans.FindByOwnerAndWeekAsync(owner, week, cancellationToken);
        if (plan is null)
        {
            plan = WeeklyPlan.Create(owner, week);
            plan.AssignRecipe(command.Day, command.RecipeIdentifier, command.RecipeTitle, command.Servings);
            await plans.AddAsync(plan, cancellationToken);
        }
        else
        {
            plan = await plans.GetByOwnerAndWeekAsync(owner, week, cancellationToken);
            plan.AssignRecipe(command.Day, command.RecipeIdentifier, command.RecipeTitle, command.Servings);
            await plans.SaveChangesAsync(cancellationToken);
        }

        var slots = plan.Slots
            .OrderBy(s => s.Day)
            .Select(s => new MealSlotDto(s.Day.ToString(), s.RecipeIdentifier, s.RecipeTitle, s.Servings, s.IsEmpty))
            .ToList();

        return new WeeklyPlanDto(week.Value, slots);
    }
}
