using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;

public sealed class SwapMealSlotsCommandHandler(IMealPlanEventStore eventStore, ICurrentUser currentUser)
	: ICommandHandler<SwapMealSlotsCommand, Result<WeeklyPlanDto>>
{
	public async Task<Result<WeeklyPlanDto>> HandleAsync(SwapMealSlotsCommand command, CancellationToken cancellationToken = default)
	{
		var (week, sourceDay, targetDay, mealType) = command;
		var owner = currentUser.AsOwner();

		var plan = await eventStore.LoadAsync(owner, week, cancellationToken)
			?? throw new EntityNotFoundException(nameof(WeeklyPlan), $"{owner.Value}/{week.Value}");

		plan.SwapSlots(sourceDay, targetDay, mealType);
		await eventStore.SaveAsync(plan, cancellationToken);

		return new WeeklyPlanDto(week.Value, plan.IsExtendedMode, plan.GetVisibleSlots().ToOrderedDtos());
	}
}
