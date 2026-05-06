using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger.Events;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

#pragma warning disable SA1601
public sealed partial class ShoppingEventStore(ShoppingDbContext context, IEventBus eventBus, ILogger<ShoppingEventStore> logger)
	: EfCoreEventStoreBase<ShoppingLedger, ShoppingLedgerIdentifier, AggregateMetadata, StoredEvent>(
		context,
		eventBus,
		ShoppingLedgerEventSerializer.Instance),
	IShoppingEventStore
{
	public async Task<ShoppingLedger> LoadAsync(OwnerIdentifier ownerId, CancellationToken cancellationToken = default)
		=> await FindAsync(ownerId, cancellationToken)
			?? throw new EntityNotFoundException(nameof(ShoppingLedger), ownerId.Value);

	public async Task<ShoppingLedger?> FindAsync(OwnerIdentifier ownerId, CancellationToken cancellationToken = default)
	{
		var ownerIdValue = ownerId.Value;
		var metadata = await Context.Set<AggregateMetadata>()
			.AsNoTracking()
			.FirstOrDefaultAsync(m => m.OwnerId == ownerIdValue, cancellationToken);

		if (metadata is null) return null;

		var events = await LoadEventsAsync(metadata.AggregateId, cancellationToken);
		return ShoppingLedger.FromEvents(ShoppingLedgerIdentifier.From(metadata.AggregateId), ownerId, events);
	}

	protected override string AggregateName => nameof(ShoppingLedger);

	protected override Guid GetAggregateId(ShoppingLedger aggregate) => aggregate.Identifier;

	protected override AggregateMetadata BuildMetadata(ShoppingLedger aggregate) =>
		new()
		{
			AggregateId = aggregate.Identifier,
			OwnerId = aggregate.OwnerId,
		};

	protected override async Task PublishEventsAsync(
		ShoppingLedger aggregate,
		IReadOnlyList<IDomainEvent> events,
		CancellationToken cancellationToken)
	{
		LogEventsSaved(aggregate.OwnerId, events.Count, aggregate.Version);

		foreach (var @event in events)
		{
			IModuleEvent? moduleEvent = @event switch
			{
				ShoppingItemAdded added => new ShoppingItemAddedModuleEvent(aggregate.OwnerId, added),
				ShoppingItemBought bought => new ShoppingItemBoughtModuleEvent(aggregate.OwnerId, bought),
				ShoppingItemConsumed consumed => new ShoppingItemConsumedModuleEvent(aggregate.OwnerId, consumed),
				ShoppingItemRemoved removed => new ShoppingItemRemovedModuleEvent(aggregate.OwnerId, removed),
				ShoppingItemQuantityAdjusted adjusted => new ShoppingItemQuantityAdjustedModuleEvent(aggregate.OwnerId, adjusted),
				ShoppingItemAddedAsAtHome atHome => new ShoppingItemAddedAsAtHomeModuleEvent(aggregate.OwnerId, atHome),
				ShoppingItemUndoBought undo => new ShoppingItemUndoBoughtModuleEvent(aggregate.OwnerId, undo),
				ShoppingItemMarkedAsFrozen frozen => new ShoppingItemMarkedAsFrozenModuleEvent(aggregate.OwnerId, frozen),
				_ => null,
			};

			if (moduleEvent is not null)
			{
				await EventBus.PublishAsync(moduleEvent, cancellationToken);
			}
		}
	}

	[LoggerMessage(Level = LogLevel.Information, Message = "Saved {Count} events for owner {OwnerId}, version now {Version}")]
	private partial void LogEventsSaved(string ownerId, int count, int version);
}
