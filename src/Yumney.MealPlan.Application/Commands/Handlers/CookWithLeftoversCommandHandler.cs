using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;

public sealed class CookWithLeftoversCommandHandler(IMealPlanEventStore eventStore, ICurrentUser currentUser)
	: ICommandHandler<CookWithLeftoversCommand, Result<WeeklyPlanDto>>
{
	public async Task<Result<WeeklyPlanDto>> HandleAsync(CookWithLeftoversCommand command, CancellationToken cancellationToken = default)
	{
		var (week, cookDay, recipe, totalServings, eatServings, leftoverDay, mealType) = command;
		var owner = currentUser.AsOwner();
		var leftoverServings = totalServings.Value > eatServings.Value
			? SlotServings.From(totalServings.Value - eatServings.Value)
			: null;

		var plan = await eventStore.FindAsync(owner, week, cancellationToken) ?? WeeklyPlan.Create(owner, week);
		plan.AssignRecipe(cookDay, recipe, mealType, totalServings);

		if (leftoverServings is not null)
		{
			plan.SetLeftover(leftoverDay, cookDay, mealType, recipe.Title, mealType, leftoverServings);
		}

		await eventStore.SaveAsync(plan, cancellationToken);

		return plan.ToDto();
	}
}
