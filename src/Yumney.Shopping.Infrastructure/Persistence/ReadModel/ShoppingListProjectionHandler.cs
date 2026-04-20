using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;

/// <summary>
/// Async projection handler — rebuilds the read model from shopping events.
/// Subscribes to integration events published by the event store.
/// See PROFILING.md alongside this file for the expected query budget per
/// event and how to snapshot it with QueryCountingInterceptor.
/// </summary>
public sealed class ShoppingListProjectionHandler(ShoppingDbContext context)
	: IIntegrationEventHandler<ShoppingItemAddedIntegrationEvent>,
	  IIntegrationEventHandler<ShoppingItemBoughtIntegrationEvent>,
	  IIntegrationEventHandler<ShoppingItemConsumedIntegrationEvent>,
	  IIntegrationEventHandler<ShoppingItemRemovedIntegrationEvent>,
	  IIntegrationEventHandler<ShoppingItemQuantityAdjustedIntegrationEvent>
{
	/// <inheritdoc />
	public Task HandleAsync(ShoppingItemAddedIntegrationEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;

		Action<ShoppingListReadItem> mutate = readItem =>
		{
			readItem.TotalQuantity += inner.Quantity.Amount;
			AppendSource(readItem, inner.Quantity.Amount, inner.Source);
		};

		return UpsertAsync(@event.OwnerId, inner.ItemName, inner.Quantity.Unit?.Value, mutate, cancellationToken);
	}

	/// <inheritdoc />
	public Task HandleAsync(ShoppingItemBoughtIntegrationEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;

		Action<ShoppingListReadItem> mutate = readItem =>
		{
			readItem.IsBought = true;
			readItem.BoughtAt = DateTime.UtcNow;
		};

		return UpdateAsync(@event.OwnerId, inner.ItemName, inner.Quantity.Unit?.Value, mutate, cancellationToken);
	}

	/// <inheritdoc />
	public Task HandleAsync(ShoppingItemConsumedIntegrationEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		return UpdateAsync(@event.OwnerId, inner.ItemName, inner.Quantity.Unit?.Value, _ => { }, cancellationToken);
	}

	/// <inheritdoc />
	public Task HandleAsync(ShoppingItemRemovedIntegrationEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;

		Action<ShoppingListReadItem> mutate = readItem =>
		{
			readItem.TotalQuantity = Math.Max(0, readItem.TotalQuantity - inner.Quantity.Amount);
			if (readItem.TotalQuantity <= 0)
			{
				context.Set<ShoppingListReadItem>().Remove(readItem);
			}
		};

		return UpdateAsync(@event.OwnerId, inner.ItemName, inner.Quantity.Unit?.Value, mutate, cancellationToken);
	}

	/// <inheritdoc />
	public Task HandleAsync(ShoppingItemQuantityAdjustedIntegrationEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;

		Action<ShoppingListReadItem> mutate = readItem =>
		{
			readItem.TotalQuantity = inner.NewQuantity.Amount;
		};

		return UpdateAsync(@event.OwnerId, inner.ItemName, inner.NewQuantity.Unit?.Value, mutate, cancellationToken);
	}

	private async Task UpdateAsync(
		string ownerId,
		string itemName,
		string? unit,
		Action<ShoppingListReadItem> mutate,
		CancellationToken cancellationToken)
	{
		var readItem = await FindAsync(ownerId, itemName, unit, cancellationToken);
		if (readItem is null) return;

		mutate(readItem);
		readItem.LastUpdated = DateTime.UtcNow;
		await context.SaveChangesAsync(cancellationToken);
	}

	private async Task UpsertAsync(
		string ownerId,
		string itemName,
		string? unit,
		Action<ShoppingListReadItem> mutate,
		CancellationToken cancellationToken)
	{
		var readItem = await FindAsync(ownerId, itemName, unit, cancellationToken)
			?? Create(ownerId, itemName, unit);
		mutate(readItem);
		readItem.LastUpdated = DateTime.UtcNow;
		await context.SaveChangesAsync(cancellationToken);
	}

	private ShoppingListReadItem Create(string ownerId, string itemName, string? unit)
	{
		var category = IngredientCategoryResolver.Resolve(itemName) ?? IngredientCategory.Other;
		var readItem = new ShoppingListReadItem
		{
			Id = Guid.CreateVersion7(),
			OwnerId = ownerId,
			ItemName = itemName,
			Unit = unit,
			Category = category.Value,
			LastUpdated = DateTime.UtcNow,
		};
		context.Set<ShoppingListReadItem>().Add(readItem);
		return readItem;
	}

	private async Task<ShoppingListReadItem?> FindAsync(string ownerId, string itemName, string? unit, CancellationToken cancellationToken)
	{
		return await context.Set<ShoppingListReadItem>()
			.FirstOrDefaultAsync(
				r => r.OwnerId == ownerId
					&& EF.Functions.ILike(r.ItemName, itemName)
					&& r.Unit == unit,
				cancellationToken);
	}

#pragma warning disable SA1204
	private static void AppendSource(ShoppingListReadItem readItem, decimal quantity, string source)
	{
		var sources = JsonSerializer.Deserialize<List<SourceEntry>>(readItem.SourcesJson) ?? [];
		sources.Add(new SourceEntry(quantity, source, DateTime.UtcNow));
		readItem.SourcesJson = JsonSerializer.Serialize(sources);
	}

	private sealed record SourceEntry(decimal Quantity, string Source, DateTime OccurredAt);
}
