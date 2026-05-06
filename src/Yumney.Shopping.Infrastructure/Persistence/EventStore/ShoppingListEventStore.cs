using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.CrossModule;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

#pragma warning disable SA1601
public sealed partial class ShoppingListEventStore(ShoppingDbContext context, IEventBus eventBus, ILogger<ShoppingListEventStore> logger)
	: EfCoreEventStoreBase<ShoppingList, ShoppingListIdentifier, ShoppingListAggregateMetadata, ShoppingListStoredEvent>(
		context,
		eventBus,
		ShoppingListEventSerializer.Instance),
	IShoppingListEventStore
{
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

	protected override string AggregateName => nameof(ShoppingList);

	protected override Guid GetAggregateId(ShoppingList aggregate) => aggregate.Identifier.Value;

	protected override ShoppingListAggregateMetadata BuildMetadata(ShoppingList aggregate) =>
		new()
		{
			AggregateId = aggregate.Identifier.Value,
			OwnerId = aggregate.Owner.Value,
		};

	protected override async Task PublishEventsAsync(
		ShoppingList aggregate,
		IReadOnlyList<IDomainEvent> events,
		CancellationToken cancellationToken)
	{
		var ownerId = aggregate.Owner.Value;
		var aggregateId = aggregate.Identifier.Value;

		LogEventsSaved(aggregateId, events.Count, aggregate.Version);

		foreach (var domainEvent in events)
		{
			var moduleEvent = ShoppingListModuleEventMapper.Map(ownerId, aggregateId, domainEvent);
			if (moduleEvent is not null)
			{
				await EventBus.PublishAsync(moduleEvent, cancellationToken);
			}

			var crossModuleEvent = MapToCrossModuleEvent(ownerId, aggregateId, domainEvent);
			if (crossModuleEvent is not null)
			{
				await EventBus.PublishAsync(crossModuleEvent, cancellationToken);
			}
		}
	}

	private static IIntegrationEvent? MapToCrossModuleEvent(string ownerId, Guid aggregateId, IDomainEvent domainEvent) =>
		domainEvent switch
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

	[LoggerMessage(Level = LogLevel.Information, Message = "Saved {Count} ShoppingList events for aggregate {AggregateId}, version now {Version}")]
	private partial void LogEventsSaved(Guid aggregateId, int count, int version);
}
