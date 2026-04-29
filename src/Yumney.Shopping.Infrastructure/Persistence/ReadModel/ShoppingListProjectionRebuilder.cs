using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;

#pragma warning disable SA1601
public sealed partial class ShoppingListProjectionRebuilder(
	ShoppingDbContext context,
	ShoppingListProjection projection,
	ILogger<ShoppingListProjectionRebuilder> logger) : IShoppingListProjectionRebuilder
{
	public async Task<int> RebuildAsync(CancellationToken cancellationToken = default)
	{
		LogStarting();

		// Truncate projection tables in dependency-safe order. Items first since
		// the summary row is the conceptual parent (no FK today, but kept for clarity).
		await context.Database.ExecuteSqlRawAsync(
			@"TRUNCATE TABLE ""ShoppingListItemReadItems"", ""ShoppingListSummaryReadItems"" RESTART IDENTITY",
			cancellationToken);

		var ownerByAggregate = await context.Set<ShoppingListAggregateMetadata>()
			.AsNoTracking()
			.ToDictionaryAsync(m => m.AggregateId, m => m.OwnerId, cancellationToken);

		var storedEvents = await context.Set<ShoppingListStoredEvent>()
			.AsNoTracking()
			.OrderBy(e => e.AggregateId)
			.ThenBy(e => e.Version)
			.ToListAsync(cancellationToken);

		var replayed = 0;
		var skippedUnknownType = 0;
		var skippedMissingOwner = 0;

		foreach (var stored in storedEvents)
		{
			if (!ownerByAggregate.TryGetValue(stored.AggregateId, out var ownerId))
			{
				skippedMissingOwner++;
				continue;
			}

			var domainEvent = ShoppingListEventSerializer.Deserialize(stored.EventType, stored.EventData);
			if (domainEvent is null)
			{
				skippedUnknownType++;
				continue;
			}

			var integrationEvent = ShoppingListIntegrationEventMapper.Map(ownerId, stored.AggregateId, domainEvent);
			if (integrationEvent is null)
			{
				skippedUnknownType++;
				continue;
			}

			switch (integrationEvent)
			{
				case ShoppingListCreatedIntegrationEvent e:
					await projection.HandleAsync(e, cancellationToken);
					break;
				case ListItemAddedIntegrationEvent e:
					await projection.HandleAsync(e, cancellationToken);
					break;
				case ListItemCheckedIntegrationEvent e:
					await projection.HandleAsync(e, cancellationToken);
					break;
				case ListItemUncheckedIntegrationEvent e:
					await projection.HandleAsync(e, cancellationToken);
					break;
				case AllItemsCheckedIntegrationEvent e:
					await projection.HandleAsync(e, cancellationToken);
					break;
				case AllItemsUncheckedIntegrationEvent e:
					await projection.HandleAsync(e, cancellationToken);
					break;
				case RecipeReferenceClearedIntegrationEvent e:
					await projection.HandleAsync(e, cancellationToken);
					break;
				default:
					skippedUnknownType++;
					continue;
			}

			replayed++;
		}

		LogFinished(replayed, skippedUnknownType, skippedMissingOwner);
		return replayed;
	}

	[LoggerMessage(Level = LogLevel.Information, Message = "ShoppingList projection rebuild starting — truncating projection tables")]
	private partial void LogStarting();

	[LoggerMessage(Level = LogLevel.Information, Message = "ShoppingList projection rebuild finished. Replayed={Replayed} SkippedUnknownType={SkippedUnknownType} SkippedMissingOwner={SkippedMissingOwner}")]
	private partial void LogFinished(int replayed, int skippedUnknownType, int skippedMissingOwner);
}
