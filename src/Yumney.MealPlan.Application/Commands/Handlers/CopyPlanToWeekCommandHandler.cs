using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;

public sealed class CopyPlanToWeekCommandHandler(IMealPlanEventStore eventStore, ICurrentUser currentUser)
	: ICommandHandler<CopyPlanToWeekCommand, Result<WeeklyPlanDto>>
{
	public async Task<Result<WeeklyPlanDto>> HandleAsync(CopyPlanToWeekCommand command, CancellationToken cancellationToken = default)
	{
		var (sourceWeek, targetWeek) = command;
		if (sourceWeek == targetWeek) return CopyPlanToWeekErrors.SameWeek;

		var owner = currentUser.AsOwner();

		var source = await eventStore.FindAsync(owner, sourceWeek, cancellationToken);
		if (source is null) return CopyPlanToWeekErrors.SourcePlanNotFound;

		var target = await eventStore.FindAsync(owner, targetWeek, cancellationToken)
			?? WeeklyPlan.Create(owner, targetWeek);

		if (source.IsExtendedMode && !target.IsExtendedMode) target.EnableExtendedMode();

		foreach (var slot in source.Slots)
		{
			ApplySlot(target, slot);
		}

		await eventStore.SaveAsync(target, cancellationToken);

		return target.ToDto();
	}

	private static void ApplySlot(WeeklyPlan target, MealSlot slot)
	{
		// Leftover slots reference a specific source meal in their original
		// week — that source isn't present in the target week, so dropping
		// them is semantically correct. Empty slots are skipped naturally.
		switch (slot.ContentType)
		{
			case SlotContentType.Recipe when slot.Recipe is not null:
				target.AssignRecipe(slot.Day, slot.Recipe, slot.MealType, slot.Servings);
				break;
			case SlotContentType.Freetext when slot.FreetextLabel is not null:
				target.SetFreetext(slot.Day, slot.FreetextLabel, slot.MealType);
				break;
			default:
				break;
		}
	}
}
