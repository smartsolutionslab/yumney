using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Events;

namespace SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;

// EventStoreBase is the abstract shared scaffold for every module's
// event store; meaningful coverage requires a real Postgres + outbox
// (concurrency conflicts, serializer roundtrip, module/cross-module
// event mapping). The end-to-end behaviour is covered by
// Yumney.Integration.Tests (ShoppingEventStoreTests,
// ShoppingListEventStoreTests, OutboxDeliveryTests). Unit tests of an
// open-generic abstract base would be ceremony, not coverage —
// exclude from the gate denominator so Shared.Persistence isn't dragged
// below threshold by code that only the integration suite can exercise.
[ExcludeFromCodeCoverage]
public abstract class EventStoreBase<TAggregate, TIdentifier, TMetadata, TStoredEvent>(
	DbContext context,
	IEventBus eventBus,
	IEventSerializer serializer,
	ILogger logger)
	where TAggregate : EventSourcedAggregate<TIdentifier>
	where TIdentifier : notnull
	where TMetadata : class, IAggregateMetadata, new()
	where TStoredEvent : class, IStoredEvent, new()
{
	public async Task SaveAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
	{
		var uncommitted = aggregate.UncommittedEvents.ToList();
		if (uncommitted.Count == 0) return;

		var aggregateId = GetAggregateId(aggregate);

		var existingMetadata = await Context.Set<TMetadata>().FirstOrDefaultAsync(m => m.AggregateId == aggregateId, cancellationToken).ConfigureAwait(false);

		if (existingMetadata is null)
		{
			Context.Set<TMetadata>().Add(BuildMetadata(aggregate));
		}

		var baseVersion = aggregate.Version - uncommitted.Count;
		for (var index = 0; index < uncommitted.Count; index++)
		{
			var @event = uncommitted[index];
			TStoredEvent storedEvent = new()
			{
				Id = Guid.CreateVersion7(),
				AggregateId = aggregateId,
				EventType = @event.GetType().Name,
				EventData = Serializer.Serialize(@event),
				Version = baseVersion + index + 1,
				OccurredAt = @event.OccurredOn,
			};
			Context.Set<TStoredEvent>().Add(storedEvent);
		}

		var busEvents = BuildBusEvents(aggregate, uncommitted);

		try
		{
			await PersistAndPublishAsync(aggregate, busEvents, cancellationToken).ConfigureAwait(false);
		}
		catch (DbUpdateException ex) when (ex.IsUniqueViolation())
		{
			throw new ConcurrencyConflictException(AggregateName, aggregateId, ex);
		}

		LogEventsSaved(aggregate, uncommitted);
	}

	protected DbContext Context { get; } = context;

	protected IEventBus EventBus { get; } = eventBus;

	protected IEventSerializer Serializer { get; } = serializer;

	protected virtual string AggregateName => typeof(TAggregate).Name;

#pragma warning disable SA1311
	private static readonly IReadOnlyDictionary<Type, ModuleEventConvention.ModuleEventFactory> emptyModuleFactories =
		new Dictionary<Type, ModuleEventConvention.ModuleEventFactory>();

	private static readonly IReadOnlyDictionary<Type, CrossModuleEventConvention.CrossModuleEventFactory> emptyCrossModuleFactories =
		new Dictionary<Type, CrossModuleEventConvention.CrossModuleEventFactory>();
#pragma warning restore SA1311

	protected virtual IReadOnlyDictionary<Type, ModuleEventConvention.ModuleEventFactory> ModuleEventFactories => emptyModuleFactories;

	protected virtual IReadOnlyDictionary<Type, CrossModuleEventConvention.CrossModuleEventFactory> CrossModuleEventFactories =>
		emptyCrossModuleFactories;

	protected abstract Guid GetAggregateId(TAggregate aggregate);

	protected abstract TMetadata BuildMetadata(TAggregate aggregate);

	protected virtual object[] BuildEventContext(TAggregate aggregate) => [];

	protected virtual IModuleEvent? MapModuleEvent(TAggregate aggregate, IDomainEvent domainEvent) => null;

	protected virtual void LogEventsSaved(TAggregate aggregate, IReadOnlyList<IDomainEvent> events)
	{
	}

	// Default implementation has a known dual-write hole: SaveChangesAsync
	// commits the events; if the bus publish that follows fails, subscribers
	// never see the message. Modules backed by a transactional outbox
	// override this to stage messages on IDbContextOutbox<T> *before*
	// SaveChangesAsync so save and publish share one transaction. Either
	// path calls MarkCommitted only after the DB commit — that way a
	// transient publish failure leaves the aggregate's uncommitted buffer
	// empty and a retry won't try to re-append the same versions.
	protected virtual async Task PersistAndPublishAsync(
		TAggregate aggregate,
		IReadOnlyList<IBusEvent> busEvents,
		CancellationToken cancellationToken)
	{
		await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

		aggregate.MarkCommitted();

		foreach (var busEvent in busEvents)
		{
			await EventBus.PublishAsync(busEvent, cancellationToken).ConfigureAwait(false);
		}
	}

	protected async Task<IReadOnlyList<IDomainEvent>> LoadEventsAsync(Guid aggregateId, CancellationToken cancellationToken)
	{
		var stored = await Context.Set<TStoredEvent>()
			.AsNoTracking()
			.Where(row => row.AggregateId == aggregateId)
			.OrderBy(row => row.Version)
			.ToListAsync(cancellationToken).ConfigureAwait(false);

		List<IDomainEvent> events = new(stored.Count);
		HashSet<string>? unknownTypes = null;
		foreach (var row in stored)
		{
			var deserialized = Serializer.Deserialize(row.EventType, row.EventData);
			if (deserialized is null)
			{
				(unknownTypes ??= []).Add(row.EventType);
				continue;
			}

			events.Add(deserialized);
		}

		if (unknownTypes is not null)
		{
			logger.LogWarning(
				"{Aggregate} {AggregateId}: skipped {Count} unknown stored event type(s): {Types}",
				AggregateName,
				aggregateId,
				unknownTypes.Count,
				string.Join(", ", unknownTypes));
		}

		return events;
	}

	private List<IBusEvent> BuildBusEvents(TAggregate aggregate, IReadOnlyList<IDomainEvent> events)
	{
		var eventContext = BuildEventContext(aggregate);
		var moduleFactories = ModuleEventFactories;
		var crossFactories = CrossModuleEventFactories;
		List<IBusEvent> busEvents = [];

		foreach (var domainEvent in events)
		{
			var moduleEvent = MapModuleEvent(aggregate, domainEvent);
			if (moduleEvent is null && moduleFactories.TryGetValue(domainEvent.GetType(), out var moduleFactory))
			{
				moduleEvent = moduleFactory(eventContext, domainEvent);
			}

			if (moduleEvent is not null)
			{
				busEvents.Add(moduleEvent);
			}

			if (crossFactories.TryGetValue(domainEvent.GetType(), out var crossFactory))
			{
				var crossEvent = crossFactory(eventContext, domainEvent);
				if (crossEvent is not null)
				{
					busEvents.Add(crossEvent);
				}
			}
		}

		return busEvents;
	}
}
