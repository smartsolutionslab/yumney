using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;

public sealed class SwapMealSlotsCommandHandler(IMealPlanUnitOfWork unitOfWork, ICurrentUser currentUser)
	: ICommandHandler<SwapMealSlotsCommand, Result<WeeklyPlanDto>>
{
	public async Task<Result<WeeklyPlanDto>> HandleAsync(SwapMealSlotsCommand command, CancellationToken cancellationToken = default)
	{
		var (week, sourceDay, targetDay, mealType) = command;
		var owner = currentUser.AsOwner();

		var plan = await unitOfWork.Plans.GetByOwnerAndWeekAsync(owner, week, cancellationToken);
		plan.SwapSlots(sourceDay, targetDay, mealType);

		await unitOfWork.SaveChangesAsync(cancellationToken);

		return new WeeklyPlanDto(week.Value, plan.IsExtendedMode, plan.GetVisibleSlots().ToOrderedDtos());
	}
}
