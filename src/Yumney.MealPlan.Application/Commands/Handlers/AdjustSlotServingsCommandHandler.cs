using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;

public sealed class AdjustSlotServingsCommandHandler(IMealPlanEventStore eventStore, ICurrentUser currentUser)
	: ICommandHandler<AdjustSlotServingsCommand, Result<WeeklyPlanDto>>
{
	public async Task<Result<WeeklyPlanDto>> HandleAsync(AdjustSlotServingsCommand command, CancellationToken cancellationToken = default)
	{
		var (week, day, mealType, servings) = command;
		var owner = currentUser.AsOwner();

		var plan = await eventStore.LoadAsync(owner, week, cancellationToken)
			?? throw new EntityNotFoundException(nameof(WeeklyPlan), $"{owner.Value}/{week.Value}");

		plan.AdjustServings(day, servings, mealType);
		await eventStore.SaveAsync(plan, cancellationToken);

		return plan.ToDto(week);
	}
}
