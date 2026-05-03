using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.CrossModule;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

#pragma warning disable SA1601
public sealed partial class EfCoreShoppingListEventStore(
	ShoppingDbContext context,
	IEventBus eventBus,
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
			.Where(stored => stored.AggregateId == aggregateId)
			.OrderBy(stored => stored.Version)
			.ToListAsync(cancellationToken);

		if (storedEvents.Count == 0) return null;

		var events = storedEvents
			.Select(stored => ShoppingListEventSerializer.Deserialize(stored.EventType, stored.EventData))
			.Where(deserialized => deserialized is not null)
			.Cast<IDomainEvent>();

		return ShoppingList.FromEvents(identifier, events);
	}

	public async Task SaveAsync(ShoppingList list, CancellationToken cancellationToken = default)
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

		try
		{
			await context.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException ex) when (ex.IsUniqueViolation())
		{
			throw new ConcurrencyConflictException(nameof(ShoppingList), aggregateId, ex);
		}

		// Mark committed BEFORE publishing: events are persisted, so the aggregate is
		// "clean" from its own perspective. A publish failure (e.g. projection handler
		// throws) propagates to the caller; the aggregate doesn't keep stale events
		// that would re-stage with conflicting versions on retry.
		list.MarkCommitted();

		await PublishEventsAsync(list, uncommitted, cancellationToken);

		LogEventsSaved(aggregateId, uncommitted.Count, list.Version);
	}

	private static IIntegrationEvent? MapToCrossModuleEvent(string ownerId, Guid aggregateId, IDomainEvent domainEvent)
	{
		return domainEvent switch
		{
			ShoppingListCreated created => new ShoppingListCreatedCrossModuleIntegrationEvent(
				ownerId,
				aggregateId,
				created.Title.Value,
				created.RecipeReference?.Value,
				created.CreatedAt),
			RecipeReferenceCleared => new ShoppingListRecipeReferenceClearedCrossModuleIntegrationEvent(
				ownerId,
				aggregateId),
			_ => null,
		};
	}

	private async Task PublishEventsAsync(ShoppingList list, List<IDomainEvent> events, CancellationToken cancellationToken)
	{
		var ownerId = list.Owner.Value;
		var aggregateId = list.Identifier.Value;

		foreach (var domainEvent in events)
		{
			var moduleEvent = ShoppingListModuleEventMapper.Map(ownerId, aggregateId, domainEvent);
			if (moduleEvent is not null)
			{
				await eventBus.PublishAsync(moduleEvent, cancellationToken);
			}

			var crossModuleEvent = MapToCrossModuleEvent(ownerId, aggregateId, domainEvent);
			if (crossModuleEvent is not null)
			{
				await eventBus.PublishAsync(crossModuleEvent, cancellationToken);
			}
		}
	}

	[LoggerMessage(Level = LogLevel.Information, Message = "Saved {Count} ShoppingList events for aggregate {AggregateId}, version now {Version}")]
	private partial void LogEventsSaved(Guid aggregateId, int count, int version);
}
