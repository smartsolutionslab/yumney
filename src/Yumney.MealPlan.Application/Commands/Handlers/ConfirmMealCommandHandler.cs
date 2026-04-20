using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;

public sealed class ConfirmMealCommandHandler(IMealPlanUnitOfWork unitOfWork, ICurrentUser currentUser)
	: ICommandHandler<ConfirmMealCommand, Result<WeeklyPlanDto>>
{
	public async Task<Result<WeeklyPlanDto>> HandleAsync(ConfirmMealCommand command, CancellationToken cancellationToken = default)
	{
		var (week, day, mealType, newState) = command;
		var owner = currentUser.AsOwner();

		var plan = await unitOfWork.Plans.GetByOwnerAndWeekAsync(owner, week, cancellationToken);

		switch (newState)
		{
			case MealState.Cooked:
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

		return new WeeklyPlanDto(week.Value, plan.IsExtendedMode, plan.GetVisibleSlots().ToOrderedDtos());
	}
}
