using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Persistence;
using SmartSolutionsLab.Yumney.Shared.Quantities;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore.Events;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;

/// <summary>
/// Async projection handler for the <c>ShoppingLedger</c> aggregate. Subscribes
/// to ledger integration events (<c>ShoppingItemAdded</c>, <c>Bought</c>,
/// <c>Consumed</c>, <c>Removed</c>, <c>QuantityAdjusted</c>) and maintains the
/// per-item rolled-up read row exposed via <see cref="MergedShoppingListDto"/>.
/// Distinct from <see cref="ShoppingListProjection"/>, which projects the
/// ShoppingList aggregate's own event stream.
/// See PROFILING.md alongside this file for the expected query budget per event.
/// </summary>
public sealed class ShoppingLedgerProjectionHandler(ShoppingDbContext context)
	: IModuleEventHandler<ShoppingItemAddedModuleEvent>,
	  IModuleEventHandler<ShoppingItemBoughtModuleEvent>,
	  IModuleEventHandler<ShoppingItemConsumedModuleEvent>,
	  IModuleEventHandler<ShoppingItemRemovedModuleEvent>,
	  IModuleEventHandler<ShoppingItemQuantityAdjustedModuleEvent>
{
	public Task HandleAsync(ShoppingItemAddedModuleEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		return UpsertItemAsync(
			@event.OwnerId,
			inner.ItemName,
			inner.Quantity.Unit?.Value,
			readItem =>
			{
				readItem.TotalQuantity += inner.Quantity.Amount;
				AppendSource(readItem, inner.Quantity.Amount, inner.Source);
			},
			cancellationToken);
	}

	public Task HandleAsync(ShoppingItemBoughtModuleEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		return UpdateItemAsync(
			@event.OwnerId,
			inner.ItemName,
			inner.Quantity.Unit?.Value,
			readItem =>
			{
				readItem.IsBought = true;
				readItem.BoughtAt = DateTime.UtcNow;
			},
			cancellationToken);
	}

	public Task HandleAsync(ShoppingItemConsumedModuleEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		return UpdateItemAsync(@event.OwnerId, inner.ItemName, inner.Quantity.Unit?.Value, _ => { }, cancellationToken);
	}

	public Task HandleAsync(ShoppingItemRemovedModuleEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		return UpdateItemAsync(
			@event.OwnerId,
			inner.ItemName,
			inner.Quantity.Unit?.Value,
			readItem =>
			{
				readItem.TotalQuantity = Math.Max(0, readItem.TotalQuantity - inner.Quantity.Amount);
				if (readItem.TotalQuantity <= 0)
				{
					context.Set<ShoppingLedgerReadItem>().Remove(readItem);
				}
			},
			cancellationToken);
	}

	public Task HandleAsync(ShoppingItemQuantityAdjustedModuleEvent @event, CancellationToken cancellationToken = default)
	{
		var inner = @event.Inner;
		return UpdateItemAsync(
			@event.OwnerId,
			inner.ItemName,
			inner.NewQuantity.Unit?.Value,
			readItem => readItem.TotalQuantity = inner.NewQuantity.Amount,
			cancellationToken);
	}

	private Task UpsertItemAsync(
		string ownerId,
		string itemName,
		string? unit,
		Action<ShoppingLedgerReadItem> mutate,
		CancellationToken cancellationToken) =>
		context.UpsertAsync<ShoppingLedgerReadItem>(
			row => row.OwnerId == ownerId && EF.Functions.ILike(row.ItemName, itemName) && row.Unit == unit,
			() => new ShoppingLedgerReadItem
			{
				Id = Guid.CreateVersion7(),
				OwnerId = ownerId,
				ItemName = itemName,
				Unit = unit,
				Category = (IngredientCategoryResolver.Resolve(itemName) ?? IngredientCategory.Other).Value,
				LastUpdated = DateTime.UtcNow,
			},
			row =>
			{
				mutate(row);
				row.LastUpdated = DateTime.UtcNow;
			},
			cancellationToken);

	private Task UpdateItemAsync(
		string ownerId,
		string itemName,
		string? unit,
		Action<ShoppingLedgerReadItem> mutate,
		CancellationToken cancellationToken) =>
		context.UpdateAsync<ShoppingLedgerReadItem>(
			row => row.OwnerId == ownerId && EF.Functions.ILike(row.ItemName, itemName) && row.Unit == unit,
			row =>
			{
				mutate(row);
				row.LastUpdated = DateTime.UtcNow;
			},
			cancellationToken);

#pragma warning disable SA1204
	private static void AppendSource(ShoppingLedgerReadItem readItem, decimal quantity, string source)
	{
		var sources = JsonSerializer.Deserialize<List<SourceEntry>>(readItem.SourcesJson) ?? [];
		sources.Add(new SourceEntry(quantity, source, DateTime.UtcNow));
		readItem.SourcesJson = JsonSerializer.Serialize(sources);
	}
#pragma warning restore SA1204

	private sealed record SourceEntry(decimal Quantity, string Source, DateTime OccurredAt);
}
