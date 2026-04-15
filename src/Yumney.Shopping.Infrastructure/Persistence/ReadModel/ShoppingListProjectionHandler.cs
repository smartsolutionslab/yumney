using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;

/// <summary>
/// Async projection handler — rebuilds the read model from shopping events.
/// Subscribes to integration events published by the event store.
/// </summary>
public sealed class ShoppingListProjectionHandler(ShoppingDbContext context)
    : IIntegrationEventHandler<ShoppingItemAddedIntegrationEvent>,
      IIntegrationEventHandler<ShoppingItemBoughtIntegrationEvent>,
      IIntegrationEventHandler<ShoppingItemConsumedIntegrationEvent>,
      IIntegrationEventHandler<ShoppingItemRemovedIntegrationEvent>,
      IIntegrationEventHandler<ShoppingItemQuantityAdjustedIntegrationEvent>
{
    /// <inheritdoc />
    public async Task HandleAsync(ShoppingItemAddedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        var inner = @event.Inner;
        var readItem = await FindOrCreateAsync(@event.OwnerId, inner.ItemName, inner.Unit?.Value, cancellationToken);
        readItem.TotalQuantity += inner.Quantity;
        readItem.LastUpdated = DateTime.UtcNow;

        AppendSource(readItem, inner.Quantity, inner.Source);

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(ShoppingItemBoughtIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        var inner = @event.Inner;
        var readItem = await FindAsync(@event.OwnerId, inner.ItemName, inner.Unit?.Value, cancellationToken);
        if (readItem is null) return;

        readItem.IsBought = true;
        readItem.BoughtAt = DateTime.UtcNow;
        readItem.LastUpdated = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(ShoppingItemConsumedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        var inner = @event.Inner;
        var readItem = await FindAsync(@event.OwnerId, inner.ItemName, inner.Unit?.Value, cancellationToken);
        if (readItem is null) return;

        readItem.LastUpdated = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(ShoppingItemRemovedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        var inner = @event.Inner;
        var readItem = await FindAsync(@event.OwnerId, inner.ItemName, inner.Unit?.Value, cancellationToken);
        if (readItem is null) return;

        readItem.TotalQuantity = Math.Max(0, readItem.TotalQuantity - inner.Quantity);
        readItem.LastUpdated = DateTime.UtcNow;

        if (readItem.TotalQuantity <= 0)
            context.Set<ShoppingListReadItem>().Remove(readItem);

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(ShoppingItemQuantityAdjustedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        var inner = @event.Inner;
        var readItem = await FindAsync(@event.OwnerId, inner.ItemName, inner.Unit?.Value, cancellationToken);
        if (readItem is null) return;

        readItem.TotalQuantity = inner.NewQuantity;
        readItem.LastUpdated = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<ShoppingListReadItem> FindOrCreateAsync(string ownerId, string itemName, string? unit, CancellationToken cancellationToken)
    {
        var existing = await FindAsync(ownerId, itemName, unit, cancellationToken);
        if (existing is not null) return existing;

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
