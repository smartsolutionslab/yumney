using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.ReadModel;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence;

/// <summary>
/// EF-backed implementation of <see cref="IMealPlanUserDataPurger"/>. Resolves
/// the owner's aggregate IDs from the metadata table, deletes their event
/// streams, then drops the metadata rows and the owner-scoped read models.
/// </summary>
public sealed class MealPlanUserDataPurger(MealPlanDbContext writeContext, MealPlanReadDbContext readContext)
	: IMealPlanUserDataPurger
{
	public async Task PurgeAsync(OwnerIdentifier owner, CancellationToken cancellationToken = default)
	{
		var ownerValue = owner.Value;

		var aggregateIds = await writeContext.MealPlanAggregates
			.Where(aggregate => aggregate.OwnerId == ownerValue)
			.Select(aggregate => aggregate.AggregateId)
			.ToListAsync(cancellationToken);

		if (aggregateIds.Count > 0)
		{
			await writeContext.MealPlanEvents
				.Where(stored => aggregateIds.Contains(stored.AggregateId))
				.ExecuteDeleteAsync(cancellationToken);
		}

		await writeContext.MealPlanAggregates
			.Where(aggregate => aggregate.OwnerId == ownerValue)
			.ExecuteDeleteAsync(cancellationToken);

		await readContext.Set<MealPlanSlotReadItem>()
			.Where(slot => slot.OwnerId == ownerValue)
			.ExecuteDeleteAsync(cancellationToken);

		await readContext.Set<MealPlanWeekReadItem>()
			.Where(week => week.OwnerId == ownerValue)
			.ExecuteDeleteAsync(cancellationToken);
	}
}
