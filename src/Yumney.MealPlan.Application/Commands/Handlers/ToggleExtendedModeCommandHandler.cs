using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;

public sealed class ToggleExtendedModeCommandHandler(IMealPlanEventStore eventStore, ICurrentUser currentUser)
	: ICommandHandler<ToggleExtendedModeCommand, Result<WeeklyPlanDto>>
{
	public async Task<Result<WeeklyPlanDto>> HandleAsync(ToggleExtendedModeCommand command, CancellationToken cancellationToken = default)
	{
		var (week, enable) = command;
		var owner = currentUser.AsOwner();

		var plan = await eventStore.LoadAsync(owner, week, cancellationToken) ?? WeeklyPlan.Create(owner, week);
		if (enable)
		{
			plan.EnableExtendedMode();
		}
		else
		{
			plan.DisableExtendedMode();
		}

		await eventStore.SaveAsync(plan, cancellationToken);

		return plan.ToDto();
	}
}
