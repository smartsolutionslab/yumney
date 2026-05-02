using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.Events;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;

public sealed class ConfirmMealCommandHandler(
	IMealPlanEventStore eventStore,
	ICurrentUser currentUser,
	IRecipeIngredientLookup recipeIngredients)
	: ICommandHandler<ConfirmMealCommand, Result<WeeklyPlanDto>>
{
	public async Task<Result<WeeklyPlanDto>> HandleAsync(ConfirmMealCommand command, CancellationToken cancellationToken = default)
	{
		var (week, day, mealType, newState) = command;
		var owner = currentUser.AsOwner();

		var plan = await eventStore.LoadAsync(owner, week, cancellationToken)
			?? throw new EntityNotFoundException(nameof(WeeklyPlan), $"{owner.Value}/{week.Value}");

		switch (newState)
		{
			case MealState.Cooked:
				var slot = plan.GetVisibleSlots().FirstOrDefault(slot => slot.Day == day && slot.MealType == mealType);
				var ingredients = slot?.Recipe is not null
					? await FetchIngredientsAsync(slot.Recipe.RecipeIdentifier, cancellationToken)
					: [];
				plan.MarkAsCooked(day, mealType, ingredients);
				break;
			case MealState.Skipped:
				plan.MarkAsSkipped(day, mealType);
				break;
			case MealState.Planned:
				plan.ResetToPlanned(day, mealType);
				break;
		}

		await eventStore.SaveAsync(plan, cancellationToken);

		return plan.ToDto(week);
	}

	private async Task<IReadOnlyList<CookedIngredient>> FetchIngredientsAsync(SlotRecipeIdentifier recipe, CancellationToken cancellationToken)
	{
		var ingredients = await recipeIngredients.LookupAsync(recipe, cancellationToken);
		return ingredients.ToCookedIngredients();
	}
}
