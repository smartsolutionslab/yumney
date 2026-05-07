using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

#pragma warning disable SA1601
public sealed partial class ShoppingEventStore(ShoppingDbContext context, IEventBus eventBus, ILogger<ShoppingEventStore> logger)
	: EventStoreBase<ShoppingLedger, ShoppingLedgerIdentifier, AggregateMetadata, StoredEvent>(
		context,
		eventBus,
		ShoppingLedgerEventSerializer.Instance,
		logger),
	IShoppingEventStore
{
#pragma warning disable SA1311
	private static readonly IReadOnlyDictionary<Type, ModuleEventConvention.ModuleEventFactory> moduleEventFactories =
		ModuleEventConvention.BuildMap(typeof(ShoppingEventStore).Assembly, typeof(string));
#pragma warning restore SA1311

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

	protected override Guid GetAggregateId(ShoppingLedger aggregate) => aggregate.Identifier;

	protected override AggregateMetadata BuildMetadata(ShoppingLedger aggregate) =>
		new()
		{
			AggregateId = aggregate.Identifier,
			OwnerId = aggregate.OwnerId,
		};

	protected override IReadOnlyDictionary<Type, ModuleEventConvention.ModuleEventFactory> ModuleEventFactories => moduleEventFactories;

	protected override object[] BuildEventContext(ShoppingLedger aggregate) => [aggregate.OwnerId.Value];

	protected override void LogEventsSaved(ShoppingLedger aggregate, IReadOnlyList<IDomainEvent> events) =>
		LogEventsSavedCore(aggregate.OwnerId, events.Count, aggregate.Version);

	[LoggerMessage(Level = LogLevel.Information, Message = "Saved {Count} events for owner {OwnerId}, version now {Version}")]
	private partial void LogEventsSavedCore(string ownerId, int count, int version);
}
