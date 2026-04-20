using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;

public sealed class ToggleExtendedModeCommandHandler(IMealPlanUnitOfWork unitOfWork, ICurrentUser currentUser)
	: ICommandHandler<ToggleExtendedModeCommand, Result<WeeklyPlanDto>>
{
	public async Task<Result<WeeklyPlanDto>> HandleAsync(ToggleExtendedModeCommand command, CancellationToken cancellationToken = default)
	{
		var (week, enable) = command;
		var owner = currentUser.AsOwner();

		var plan = await unitOfWork.Plans.FindForUpdateAsync(owner, week, cancellationToken);
		if (plan is null)
		{
			plan = WeeklyPlan.Create(owner, week);

			if (enable)
			{
				plan.EnableExtendedMode();
			}

			await unitOfWork.Plans.AddAsync(plan, cancellationToken);
		}
		else
		{
			if (enable)
			{
				plan.EnableExtendedMode();
			}
			else
			{
				plan.DisableExtendedMode();
			}
		}

		await unitOfWork.SaveChangesAsync(cancellationToken);

		return new WeeklyPlanDto(week.Value, plan.IsExtendedMode, plan.GetVisibleSlots().ToOrderedDtos());
	}
}
