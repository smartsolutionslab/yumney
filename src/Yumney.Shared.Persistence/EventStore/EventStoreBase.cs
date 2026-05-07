using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Events;

namespace SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;

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

		var existingMetadata = await Context.Set<TMetadata>()
			.FirstOrDefaultAsync(m => m.AggregateId == aggregateId, cancellationToken);

		if (existingMetadata is null)
		{
			Context.Set<TMetadata>().Add(BuildMetadata(aggregate));
		}

		var baseVersion = aggregate.Version - uncommitted.Count;
		for (var index = 0; index < uncommitted.Count; index++)
		{
			var @event = uncommitted[index];
			Context.Set<TStoredEvent>().Add(new TStoredEvent
			{
				Id = Guid.CreateVersion7(),
				AggregateId = aggregateId,
				EventType = @event.GetType().Name,
				EventData = Serializer.Serialize(@event),
				Version = baseVersion + index + 1,
				OccurredAt = @event.OccurredOn,
			});
		}

		try
		{
			await Context.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException ex) when (ex.IsUniqueViolation())
		{
			throw new ConcurrencyConflictException(AggregateName, aggregateId, ex);
		}

		// Mark committed BEFORE publishing: events are persisted, so the aggregate is
		// "clean" from its own perspective. A publish failure propagates to the caller;
		// the aggregate doesn't keep stale events that would re-stage with conflicting
		// versions on retry.
		aggregate.MarkCommitted();

		await PublishEventsAsync(aggregate, uncommitted, cancellationToken);
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

	protected virtual IReadOnlyDictionary<Type, ModuleEventConvention.ModuleEventFactory> ModuleEventFactories =>
		emptyModuleFactories;

	protected virtual IReadOnlyDictionary<Type, CrossModuleEventConvention.CrossModuleEventFactory> CrossModuleEventFactories =>
		emptyCrossModuleFactories;

	protected abstract Guid GetAggregateId(TAggregate aggregate);

	protected abstract TMetadata BuildMetadata(TAggregate aggregate);

	protected virtual object[] BuildEventContext(TAggregate aggregate) => [];

	protected virtual IModuleEvent? MapModuleEvent(TAggregate aggregate, IDomainEvent domainEvent) => null;

	protected virtual void LogEventsSaved(TAggregate aggregate, IReadOnlyList<IDomainEvent> events)
	{
	}

	protected virtual async Task PublishEventsAsync(TAggregate aggregate, IReadOnlyList<IDomainEvent> events, CancellationToken cancellationToken)
	{
		LogEventsSaved(aggregate, events);

		var context = BuildEventContext(aggregate);
		var moduleFactories = ModuleEventFactories;
		var crossFactories = CrossModuleEventFactories;

		foreach (var domainEvent in events)
		{
			var moduleEvent = MapModuleEvent(aggregate, domainEvent);
			if (moduleEvent is null && moduleFactories.TryGetValue(domainEvent.GetType(), out var moduleFactory))
			{
				moduleEvent = moduleFactory(context, domainEvent);
			}

			if (moduleEvent is not null)
			{
				await EventBus.PublishAsync(moduleEvent, cancellationToken);
			}

			if (crossFactories.TryGetValue(domainEvent.GetType(), out var crossFactory))
			{
				var crossEvent = crossFactory(context, domainEvent);
				if (crossEvent is not null)
				{
					await EventBus.PublishAsync(crossEvent, cancellationToken);
				}
			}
		}
	}

	protected async Task<IReadOnlyList<IDomainEvent>> LoadEventsAsync(Guid aggregateId, CancellationToken cancellationToken)
	{
		var stored = await Context.Set<TStoredEvent>()
			.AsNoTracking()
			.Where(row => row.AggregateId == aggregateId)
			.OrderBy(row => row.Version)
			.ToListAsync(cancellationToken);

		var events = new List<IDomainEvent>(stored.Count);
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
}
