using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;
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
	public async Task PurgeAsync(string keycloakUserId, CancellationToken cancellationToken = default)
	{
		var aggregateIds = await writeContext.MealPlanAggregates
			.Where(aggregate => aggregate.OwnerId == keycloakUserId)
			.Select(aggregate => aggregate.AggregateId)
			.ToListAsync(cancellationToken);

		if (aggregateIds.Count > 0)
		{
			await writeContext.MealPlanEvents
				.Where(stored => aggregateIds.Contains(stored.AggregateId))
				.ExecuteDeleteAsync(cancellationToken);
		}

		await writeContext.MealPlanAggregates
			.Where(aggregate => aggregate.OwnerId == keycloakUserId)
			.ExecuteDeleteAsync(cancellationToken);

		await readContext.Set<MealPlanSlotReadItem>()
			.Where(slot => slot.OwnerId == keycloakUserId)
			.ExecuteDeleteAsync(cancellationToken);

		await readContext.Set<MealPlanWeekReadItem>()
			.Where(week => week.OwnerId == keycloakUserId)
			.ExecuteDeleteAsync(cancellationToken);
	}
}
