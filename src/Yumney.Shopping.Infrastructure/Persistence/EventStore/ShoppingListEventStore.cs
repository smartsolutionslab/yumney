using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

#pragma warning disable SA1601
public sealed partial class ShoppingListEventStore(ShoppingDbContext context, IEventBus eventBus, ILogger<ShoppingListEventStore> logger)
	: EfCoreEventStoreBase<ShoppingList, ShoppingListIdentifier, ShoppingListAggregateMetadata, ShoppingListStoredEvent>(
		context,
		eventBus,
		ShoppingListEventSerializer.Instance,
		logger),
	IShoppingListEventStore
{
#pragma warning disable SA1311
	private static readonly IReadOnlyDictionary<Type, CrossModuleEventConvention.CrossModuleEventFactory> crossModuleEventFactories =
		CrossModuleEventConvention.BuildMap(typeof(ShoppingListEventStore).Assembly);
#pragma warning restore SA1311

	public async Task<ShoppingList> LoadAsync(ShoppingListIdentifier identifier, CancellationToken cancellationToken = default)
		=> await FindAsync(identifier, cancellationToken)
			?? throw new EntityNotFoundException(nameof(ShoppingList), identifier.Value);

	public async Task<ShoppingList?> FindAsync(ShoppingListIdentifier identifier, CancellationToken cancellationToken = default)
	{
		var aggregateId = identifier.Value;

		var metadata = await Context.Set<ShoppingListAggregateMetadata>()
			.AsNoTracking()
			.FirstOrDefaultAsync(m => m.AggregateId == aggregateId, cancellationToken);

		if (metadata is null) return null;

		var events = await LoadEventsAsync(aggregateId, cancellationToken);
		if (events.Count == 0) return null;

		return ShoppingList.FromEvents(identifier, events);
	}

	protected override Guid GetAggregateId(ShoppingList aggregate) => aggregate.Identifier.Value;

	protected override ShoppingListAggregateMetadata BuildMetadata(ShoppingList aggregate) =>
		new()
		{
			AggregateId = aggregate.Identifier.Value,
			OwnerId = aggregate.Owner.Value,
		};

	protected override IReadOnlyDictionary<Type, CrossModuleEventConvention.CrossModuleEventFactory> CrossModuleEventFactories => crossModuleEventFactories;

	protected override object[] BuildEventContext(ShoppingList aggregate) => [aggregate.Owner.Value, aggregate.Identifier.Value];

	protected override IModuleEvent? MapModuleEvent(ShoppingList aggregate, IDomainEvent domainEvent) =>
		ShoppingListModuleEventMapper.Map(aggregate.Owner.Value, aggregate.Identifier.Value, domainEvent);

	protected override void LogEventsSaved(ShoppingList aggregate, IReadOnlyList<IDomainEvent> events) =>
		LogEventsSavedCore(aggregate.Identifier.Value, events.Count, aggregate.Version);

	[LoggerMessage(Level = LogLevel.Information, Message = "Saved {Count} ShoppingList events for aggregate {AggregateId}, version now {Version}")]
	private partial void LogEventsSavedCore(Guid aggregateId, int count, int version);
}
