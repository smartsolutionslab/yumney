using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;

public sealed class CookWithLeftoversCommandHandler(IWeeklyPlanRepository plans, ICurrentUser currentUser)
	: ICommandHandler<CookWithLeftoversCommand, Result<WeeklyPlanDto>>
{
	public async Task<Result<WeeklyPlanDto>> HandleAsync(CookWithLeftoversCommand command, CancellationToken cancellationToken = default)
	{
		var (week, cookDay, recipe, totalServings, eatServings, leftoverDay, mealType) = command;
		var owner = currentUser.AsOwner();
		var leftoverServingsValue = totalServings.Value - eatServings.Value;

		var plan = await plans.FindForUpdateAsync(owner, week, cancellationToken);
		if (plan is null)
		{
			plan = WeeklyPlan.Create(owner, week);
			plan.AssignRecipe(cookDay, recipe, mealType, totalServings);

			if (leftoverServingsValue > 0)
			{
				plan.SetLeftover(leftoverDay, cookDay, mealType, recipe.Title, mealType, SlotServings.From(leftoverServingsValue));
			}

			await plans.AddAsync(plan, cancellationToken);
		}
		else
		{
			plan.AssignRecipe(cookDay, recipe, mealType, totalServings);

			if (leftoverServingsValue > 0)
			{
				plan.SetLeftover(leftoverDay, cookDay, mealType, recipe.Title, mealType, SlotServings.From(leftoverServingsValue));
			}

			await plans.SaveChangesAsync(cancellationToken);
		}

		return new WeeklyPlanDto(week.Value, plan.IsExtendedMode, plan.GetVisibleSlots().ToOrderedDtos());
	}
}
