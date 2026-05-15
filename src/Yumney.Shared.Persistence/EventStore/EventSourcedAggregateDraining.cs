using Microsoft.EntityFrameworkCore;

namespace SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;

/// <summary>
/// Bulk-purge helpers for the event-sourced "user account deleted" path.
/// Used by every module that owns an event store (MealPlan, Shopping) to
/// satisfy the GDPR cascade — resolves the owner's aggregate ids, drops
/// the matching event streams, then drops the metadata rows. Read-model
/// rows stay the caller's responsibility because their schema varies per
/// module.
/// </summary>
public static class EventSourcedAggregateDraining
{
	public static async Task DrainOwnerAggregatesAsync<TMetadata, TStoredEvent>(
		this DbSet<TMetadata> metadataSet,
		DbSet<TStoredEvent> eventsSet,
		string ownerValue,
		CancellationToken cancellationToken)
		where TMetadata : class, IOwnerScopedAggregateMetadata
		where TStoredEvent : class, IStoredEvent
	{
		var aggregateIds = await metadataSet
			.Where(metadata => metadata.OwnerId == ownerValue)
			.Select(metadata => metadata.AggregateId)
			.ToListAsync(cancellationToken);

		if (aggregateIds.Count > 0)
		{
			await eventsSet
				.Where(stored => aggregateIds.Contains(stored.AggregateId))
				.ExecuteDeleteAsync(cancellationToken);
		}

		await metadataSet
			.Where(metadata => metadata.OwnerId == ownerValue)
			.ExecuteDeleteAsync(cancellationToken);
	}
}
