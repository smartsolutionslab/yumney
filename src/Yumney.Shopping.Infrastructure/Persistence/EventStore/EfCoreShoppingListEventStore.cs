using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

#pragma warning disable SA1601
public sealed partial class EfCoreShoppingListEventStore(
	ShoppingDbContext context,
	ILogger<EfCoreShoppingListEventStore> logger) : IShoppingListEventStore
{
	public async Task<ShoppingList?> LoadAsync(
		ShoppingListIdentifier identifier,
		CancellationToken cancellationToken = default)
	{
		var aggregateId = identifier.Value;

		var metadata = await context.Set<ShoppingListAggregateMetadata>()
			.AsNoTracking()
			.FirstOrDefaultAsync(m => m.AggregateId == aggregateId, cancellationToken);

		if (metadata is null) return null;

		var storedEvents = await context.Set<ShoppingListStoredEvent>()
			.AsNoTracking()
			.Where(e => e.AggregateId == aggregateId)
			.OrderBy(e => e.Version)
			.ToListAsync(cancellationToken);

		if (storedEvents.Count == 0) return null;

		var events = storedEvents
			.Select(s => ShoppingListEventSerializer.Deserialize(s.EventType, s.EventData))
			.Where(e => e is not null)
			.Cast<IDomainEvent>();

		return ShoppingList.FromEvents(identifier, events);
	}

	public async Task AppendAsync(ShoppingList list, CancellationToken cancellationToken = default)
	{
		var uncommitted = list.UncommittedEvents.ToList();
		if (uncommitted.Count == 0) return;

		var aggregateId = list.Identifier.Value;

		var existingMetadata = await context.Set<ShoppingListAggregateMetadata>()
			.FirstOrDefaultAsync(m => m.AggregateId == aggregateId, cancellationToken);

		if (existingMetadata is null)
		{
			context.Set<ShoppingListAggregateMetadata>().Add(new ShoppingListAggregateMetadata
			{
				AggregateId = aggregateId,
				OwnerId = list.Owner.Value,
			});
		}

		var baseVersion = list.Version - uncommitted.Count;

		for (var index = 0; index < uncommitted.Count; index++)
		{
			var @event = uncommitted[index];
			context.Set<ShoppingListStoredEvent>().Add(new ShoppingListStoredEvent
			{
				Id = Guid.CreateVersion7(),
				AggregateId = aggregateId,
				EventType = @event.GetType().Name,
				EventData = ShoppingListEventSerializer.Serialize(@event),
				Version = baseVersion + index + 1,
				OccurredAt = @event.OccurredOn,
			});
		}

		LogEventsStaged(aggregateId, uncommitted.Count, list.Version);
	}

	[LoggerMessage(Level = LogLevel.Debug, Message = "Staged {Count} ShoppingList events for aggregate {AggregateId}, version now {Version}")]
	private partial void LogEventsStaged(Guid aggregateId, int count, int version);
}
