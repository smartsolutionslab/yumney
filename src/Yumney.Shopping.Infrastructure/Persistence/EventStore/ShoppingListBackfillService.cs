using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

#pragma warning disable SA1601
public sealed partial class ShoppingListBackfillService(
	ShoppingDbContext context,
	ILogger<ShoppingListBackfillService> logger) : IShoppingListBackfillService
{
	public async Task<int> BackfillAsync(CancellationToken cancellationToken = default)
	{
		LogStarting();

		var existingAggregateIds = (await context.Set<ShoppingListAggregateMetadata>()
			.AsNoTracking()
			.Select(m => m.AggregateId)
			.ToListAsync(cancellationToken))
			.ToHashSet();

		var legacy = await context.ShoppingLists
			.AsNoTracking()
			.Include(l => l.Items)
			.ToListAsync(cancellationToken);

		var backfilled = 0;
		var skipped = 0;

		foreach (var list in legacy)
		{
			if (existingAggregateIds.Contains(list.Identifier.Value))
			{
				skipped++;
				continue;
			}

			AppendSyntheticEvents(list);
			backfilled++;
		}

		if (backfilled == 0)
		{
			LogFinished(backfilled, skipped);
			return 0;
		}

		await context.SaveChangesAsync(cancellationToken);
		LogFinished(backfilled, skipped);
		return backfilled;
	}

	private void AppendSyntheticEvents(ShoppingList list)
	{
		var aggregateId = list.Identifier.Value;
		var occurredAt = list.CreatedAt;
		var version = 0;

		context.Set<ShoppingListAggregateMetadata>().Add(new ShoppingListAggregateMetadata
		{
			AggregateId = aggregateId,
			OwnerId = list.Owner.Value,
		});

		AppendStoredEvent(
			aggregateId,
			++version,
			occurredAt,
			new ShoppingListCreated(list.Identifier, list.Title, list.Owner, list.RecipeReference, list.CreatedAt));

		foreach (var item in list.Items)
		{
			AppendStoredEvent(
				aggregateId,
				++version,
				occurredAt,
				new ListItemAdded(item.Id, item.Name, item.Quantity));
		}

		// Items in a "checked" state get a synthetic ListItemChecked emitted after their
		// ListItemAdded so replay reconstructs the same flag. Without an audit trail of
		// when the user actually checked the item we keep the OccurredAt aligned with
		// the list's CreatedAt — the column is best-effort for legacy rows.
		foreach (var item in list.Items.Where(i => i.IsChecked))
		{
			AppendStoredEvent(
				aggregateId,
				++version,
				occurredAt,
				new ListItemChecked(item.Id));
		}
	}

	private void AppendStoredEvent(Guid aggregateId, int version, DateTime occurredAt, IDomainEvent @event)
	{
		context.Set<ShoppingListStoredEvent>().Add(new ShoppingListStoredEvent
		{
			Id = Guid.CreateVersion7(),
			AggregateId = aggregateId,
			EventType = @event.GetType().Name,
			EventData = ShoppingListEventSerializer.Serialize(@event),
			Version = version,
			OccurredAt = occurredAt,
		});
	}

	[LoggerMessage(Level = LogLevel.Information, Message = "ShoppingList backfill starting")]
	private partial void LogStarting();

	[LoggerMessage(Level = LogLevel.Information, Message = "ShoppingList backfill finished. Backfilled={Backfilled} Skipped={Skipped}")]
	private partial void LogFinished(int backfilled, int skipped);
}
