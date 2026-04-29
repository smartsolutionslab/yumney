using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger.Events;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Converters;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

#pragma warning disable SA1601
public sealed partial class EfCoreShoppingEventStore(
	ShoppingDbContext context,
	IEventBus eventBus,
	ILogger<EfCoreShoppingEventStore> logger) : IShoppingEventStore
{
#pragma warning disable SA1311, SA1303, SA1204
	private static readonly JsonSerializerOptions jsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		Converters =
		{
			new ItemNameJsonConverter(),
			new AmountJsonConverter(),
			new UnitJsonConverter(),
			new RemovalReasonJsonConverter(),
			new ItemSourceJsonConverter(),
		},
	};

	private static readonly Dictionary<string, Type> eventTypeMap = new()
	{
		[nameof(ShoppingItemAdded)] = typeof(ShoppingItemAdded),
		[nameof(ShoppingItemBought)] = typeof(ShoppingItemBought),
		[nameof(ShoppingItemConsumed)] = typeof(ShoppingItemConsumed),
		[nameof(ShoppingItemRemoved)] = typeof(ShoppingItemRemoved),
		[nameof(ShoppingItemQuantityAdjusted)] = typeof(ShoppingItemQuantityAdjusted),
		[nameof(ShoppingItemUndoBought)] = typeof(ShoppingItemUndoBought),
		[nameof(ShoppingItemAddedAsAtHome)] = typeof(ShoppingItemAddedAsAtHome),
		[nameof(ShoppingModeStarted)] = typeof(ShoppingModeStarted),
		[nameof(ShoppingModeEnded)] = typeof(ShoppingModeEnded),
	};
#pragma warning restore SA1311

	public async Task<ShoppingLedger?> LoadAsync(OwnerIdentifier ownerId, CancellationToken cancellationToken = default)
	{
		var ownerIdValue = ownerId.Value;
		var metadata = await context.Set<AggregateMetadata>()
			.AsNoTracking()
			.FirstOrDefaultAsync(m => m.OwnerId == ownerIdValue, cancellationToken);

		if (metadata is null)
			return null;

		var aggregateId = metadata.AggregateId;

		var storedEvents = await context.Set<StoredEvent>()
			.AsNoTracking()
			.Where(e => e.AggregateId == aggregateId)
			.OrderBy(e => e.Version)
			.ToListAsync(cancellationToken);

		var events = storedEvents.Select(DeserializeEvent).Where(e => e is not null).Cast<IDomainEvent>();

		return ShoppingLedger.FromEvents(ShoppingLedgerIdentifier.From(aggregateId), ownerId, events);
	}

	public async Task SaveAsync(ShoppingLedger ledger, CancellationToken cancellationToken = default)
	{
		var uncommitted = ledger.UncommittedEvents.ToList();
		if (uncommitted.Count == 0) return;

		var existingMetadata = await context.Set<AggregateMetadata>()
			.FirstOrDefaultAsync(m => m.AggregateId == ledger.Identifier, cancellationToken);

		if (existingMetadata is null)
		{
			context.Set<AggregateMetadata>().Add(new AggregateMetadata
			{
				AggregateId = ledger.Identifier,
				OwnerId = ledger.OwnerId,
			});
		}

		var baseVersion = ledger.Version - uncommitted.Count;

		for (var i = 0; i < uncommitted.Count; i++)
		{
			var @event = uncommitted[i];
			context.Set<StoredEvent>().Add(new StoredEvent
			{
				Id = Guid.CreateVersion7(),
				AggregateId = ledger.Identifier,
				EventType = @event.GetType().Name,
				EventData = JsonSerializer.Serialize(@event, @event.GetType(), jsonOptions),
				Version = baseVersion + i + 1,
				OccurredAt = @event.OccurredOn,
			});
		}

		try
		{
			await context.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException ex) when (ex.IsUniqueViolation())
		{
			throw new ConcurrencyConflictException(nameof(ShoppingLedger), ledger.Identifier, ex);
		}

		ledger.MarkCommitted();

		await PublishEventsAsync(ledger.OwnerId, uncommitted, cancellationToken);

		LogEventsSaved(ledger.OwnerId, uncommitted.Count, ledger.Version);
	}

	private async Task PublishEventsAsync(string ownerId, List<IDomainEvent> events, CancellationToken cancellationToken)
	{
		foreach (var @event in events)
		{
			if (@event is ShoppingItemAdded added)
				await eventBus.PublishAsync(new ShoppingItemAddedIntegrationEvent(ownerId, added), cancellationToken);
			else if (@event is ShoppingItemBought bought)
				await eventBus.PublishAsync(new ShoppingItemBoughtIntegrationEvent(ownerId, bought), cancellationToken);
			else if (@event is ShoppingItemConsumed consumed)
				await eventBus.PublishAsync(new ShoppingItemConsumedIntegrationEvent(ownerId, consumed), cancellationToken);
			else if (@event is ShoppingItemRemoved removed)
				await eventBus.PublishAsync(new ShoppingItemRemovedIntegrationEvent(ownerId, removed), cancellationToken);
			else if (@event is ShoppingItemQuantityAdjusted adjusted)
				await eventBus.PublishAsync(new ShoppingItemQuantityAdjustedIntegrationEvent(ownerId, adjusted), cancellationToken);
		}
	}

	private static IDomainEvent? DeserializeEvent(StoredEvent stored)
	{
		if (!eventTypeMap.TryGetValue(stored.EventType, out var type)) return null;

		return JsonSerializer.Deserialize(stored.EventData, type, jsonOptions) as IDomainEvent;
	}

	[LoggerMessage(Level = LogLevel.Information, Message = "Saved {Count} events for owner {OwnerId}, version now {Version}")]
	private partial void LogEventsSaved(string ownerId, int count, int version);
}
