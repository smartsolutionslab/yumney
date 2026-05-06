using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;

public sealed class AssignRecipeCommandHandler(IMealPlanEventStore eventStore, ICurrentUser currentUser)
	: ICommandHandler<AssignRecipeCommand, Result<WeeklyPlanDto>>
{
	public async Task<Result<WeeklyPlanDto>> HandleAsync(AssignRecipeCommand command, CancellationToken cancellationToken = default)
	{
		var (week, day, recipe, mealType, servings) = command;
		var owner = currentUser.AsOwner();

		var plan = await eventStore.FindAsync(owner, week, cancellationToken) ?? WeeklyPlan.Create(owner, week);
		plan.AssignRecipe(day, recipe, mealType, servings);
		await eventStore.SaveAsync(plan, cancellationToken);

		return plan.ToDto();
	}
}
