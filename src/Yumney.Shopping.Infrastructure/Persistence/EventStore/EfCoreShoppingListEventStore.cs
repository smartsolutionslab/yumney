using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore.Json;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Converters;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

#pragma warning disable SA1601
public sealed partial class EfCoreShoppingListEventStore(
	ShoppingDbContext context,
	ILogger<EfCoreShoppingListEventStore> logger) : IShoppingListEventStore
{
#pragma warning disable SA1311, SA1303, SA1204
	private static readonly JsonSerializerOptions jsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		Converters =
		{
			new StringValueObjectJsonConverter<OwnerIdentifier>(OwnerIdentifier.From),
			new StringValueObjectJsonConverter<ShoppingListTitle>(ShoppingListTitle.From),
			new StringValueObjectJsonConverter<ItemName>(ItemName.From),
			new ShoppingListIdentifierJsonConverter(),
			new ShoppingListItemIdentifierJsonConverter(),
			new RecipeReferenceJsonConverter(),
			new AmountJsonConverter(),
			new UnitJsonConverter(),
		},
	};

	private static readonly Dictionary<string, Type> eventTypeMap = new()
	{
		[nameof(ShoppingListCreated)] = typeof(ShoppingListCreated),
		[nameof(ListItemAdded)] = typeof(ListItemAdded),
		[nameof(ListItemChecked)] = typeof(ListItemChecked),
		[nameof(ListItemUnchecked)] = typeof(ListItemUnchecked),
		[nameof(AllItemsChecked)] = typeof(AllItemsChecked),
		[nameof(AllItemsUnchecked)] = typeof(AllItemsUnchecked),
		[nameof(RecipeReferenceCleared)] = typeof(RecipeReferenceCleared),
	};
#pragma warning restore SA1311

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

		var events = storedEvents.Select(DeserializeEvent).Where(e => e is not null).Cast<IDomainEvent>();

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
				EventData = JsonSerializer.Serialize(@event, @event.GetType(), jsonOptions),
				Version = baseVersion + index + 1,
				OccurredAt = @event.OccurredOn,
			});
		}

		LogEventsStaged(aggregateId, uncommitted.Count, list.Version);
	}

	private static IDomainEvent? DeserializeEvent(ShoppingListStoredEvent stored)
	{
		if (!eventTypeMap.TryGetValue(stored.EventType, out var type)) return null;

		return JsonSerializer.Deserialize(stored.EventData, type, jsonOptions) as IDomainEvent;
	}

	[LoggerMessage(Level = LogLevel.Debug, Message = "Staged {Count} ShoppingList events for aggregate {AggregateId}, version now {Version}")]
	private partial void LogEventsStaged(Guid aggregateId, int count, int version);
}
