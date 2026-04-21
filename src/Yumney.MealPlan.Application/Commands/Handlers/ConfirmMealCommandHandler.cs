using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.CrossModule;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;

public sealed class ConfirmMealCommandHandler(
	IMealPlanUnitOfWork unitOfWork,
	ICurrentUser currentUser,
	IRecipeIngredientProvider ingredientProvider,
	IEventBus eventBus)
	: ICommandHandler<ConfirmMealCommand, Result<WeeklyPlanDto>>
{
	public async Task<Result<WeeklyPlanDto>> HandleAsync(ConfirmMealCommand command, CancellationToken cancellationToken = default)
	{
		var (week, day, mealType, newState) = command;
		var owner = currentUser.AsOwner();

		var plan = await unitOfWork.Plans.GetByOwnerAndWeekAsync(owner, week, cancellationToken);

		SlotRecipeReference? confirmedRecipe = null;
		SlotServings? confirmedServings = null;

		switch (newState)
		{
			case MealState.Cooked:
				var slot = plan.GetVisibleSlots().FirstOrDefault(s => s.Day == day && s.MealType == mealType);
				if (slot is not null)
				{
					confirmedRecipe = slot.Recipe;
					confirmedServings = slot.Servings;
				}

				plan.MarkAsCooked(day, mealType);
				break;
			case MealState.Skipped:
				plan.MarkAsSkipped(day, mealType);
				break;
			case MealState.Planned:
				plan.ResetToPlanned(day, mealType);
				break;
		}

		await unitOfWork.SaveChangesAsync(cancellationToken);

		if (newState == MealState.Cooked && confirmedRecipe is not null && confirmedServings is not null)
		{
			var ingredients = await ingredientProvider.GetIngredientsAsync(
				confirmedRecipe.RecipeIdentifier.Value,
				cancellationToken);

			var payload = ingredients
				.Where(i => i.Amount.HasValue && i.Amount.Value > 0m)
				.Select(i => new MealConfirmedIngredient(i.Name, i.Amount!.Value, i.Unit))
				.ToList();

			await eventBus.PublishAsync(
				new MealConfirmedIntegrationEvent(
					owner.Value,
					confirmedRecipe.RecipeIdentifier.Value,
					confirmedServings.Value,
					payload),
				cancellationToken);
		}

		return new WeeklyPlanDto(week.Value, plan.IsExtendedMode, plan.GetVisibleSlots().ToOrderedDtos());
	}
}
