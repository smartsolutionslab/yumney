using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.ReadModel;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence;

/// <summary>
/// EF-backed implementation of <see cref="IMealPlanUserDataPurger"/>. Drains the
/// owner's event stream + metadata via the shared
/// <see cref="EventSourcedAggregateDraining"/> helper, then deletes the
/// owner-scoped read-model rows.
/// </summary>
public sealed class MealPlanUserDataPurger(MealPlanDbContext writeContext, MealPlanReadDbContext readContext)
	: IMealPlanUserDataPurger
{
	public async Task PurgeAsync(OwnerIdentifier owner, CancellationToken cancellationToken = default)
	{
		var ownerValue = owner.Value;

		await writeContext.MealPlanAggregates.DrainOwnerAggregatesAsync(
			writeContext.MealPlanEvents,
			ownerValue,
			cancellationToken);

		await readContext.Set<MealPlanSlotReadItem>()
			.Where(slot => slot.OwnerId == ownerValue)
			.ExecuteDeleteAsync(cancellationToken);

		await readContext.Set<MealPlanWeekReadItem>()
			.Where(week => week.OwnerId == ownerValue)
			.ExecuteDeleteAsync(cancellationToken);
	}
}
