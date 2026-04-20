using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;

public sealed class AdjustSlotServingsCommandHandler(IMealPlanUnitOfWork unitOfWork, ICurrentUser currentUser)
	: ICommandHandler<AdjustSlotServingsCommand, Result<WeeklyPlanDto>>
{
	public async Task<Result<WeeklyPlanDto>> HandleAsync(AdjustSlotServingsCommand command, CancellationToken cancellationToken = default)
	{
		var (week, day, mealType, servings) = command;
		var owner = currentUser.AsOwner();

		var plan = await unitOfWork.Plans.GetByOwnerAndWeekAsync(owner, week, cancellationToken);

		plan.AdjustServings(day, servings, mealType);
		await unitOfWork.SaveChangesAsync(cancellationToken);

		return new WeeklyPlanDto(week.Value, plan.IsExtendedMode, plan.GetVisibleSlots().ToOrderedDtos());
	}
}
