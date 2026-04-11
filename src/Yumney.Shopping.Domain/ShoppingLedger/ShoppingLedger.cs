using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger.Events;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;

/// <summary>
/// Event-sourced aggregate for a user's shopping list.
/// State is rebuilt by replaying domain events.
/// One instance per user, lives forever.
/// </summary>
public sealed class ShoppingLedger
{
    private readonly List<IDomainEvent> uncommittedEvents = [];
    private readonly Dictionary<string, ShoppingItemState> items = new(StringComparer.OrdinalIgnoreCase);

    public Guid Id { get; private set; }

    public string OwnerId { get; private set; } = default!;

    public int Version { get; private set; }

    public IReadOnlyCollection<IDomainEvent> UncommittedEvents => uncommittedEvents.AsReadOnly();

    public IReadOnlyDictionary<string, ShoppingItemState> Items => items;

    public bool IsInShoppingMode { get; private set; }

    public DateTime? ShoppingModeStartedAt { get; private set; }

    public int PendingChangesCount { get; private set; }

    private ShoppingLedger()
    {
    }

    public static ShoppingLedger Create(string ownerId)
    {
        return new ShoppingLedger
        {
            Id = Guid.CreateVersion7(),
            OwnerId = ownerId,
        };
    }

    /// <summary>
    /// Rebuild aggregate from persisted events (optionally starting from a snapshot).
    /// </summary>
    /// <param name="id">The aggregate identifier.</param>
    /// <param name="ownerId">The owner user identifier.</param>
    /// <param name="events">The domain events to replay.</param>
    /// <param name="startVersion">The starting version (default 0).</param>
    /// <returns>The hydrated aggregate.</returns>
    public static ShoppingLedger FromEvents(Guid id, string ownerId, IEnumerable<IDomainEvent> events, int startVersion = 0)
    {
        var ledger = new ShoppingLedger { Id = id, OwnerId = ownerId, Version = startVersion };
        foreach (var @event in events)
        {
            ledger.Apply(@event);
            ledger.Version++;
        }

        return ledger;
    }

    /// <summary>
    /// Rebuild from snapshot state + events since snapshot.
    /// </summary>
    /// <param name="id">The aggregate identifier.</param>
    /// <param name="ownerId">The owner user identifier.</param>
    /// <param name="snapshotItems">The snapshot item state.</param>
    /// <param name="snapshotVersion">The version at snapshot time.</param>
    /// <param name="eventsSinceSnapshot">Events to replay after the snapshot.</param>
    /// <returns>The hydrated aggregate.</returns>
    public static ShoppingLedger FromSnapshot(
        Guid id,
        string ownerId,
        Dictionary<string, ShoppingItemState> snapshotItems,
        int snapshotVersion,
        IEnumerable<IDomainEvent> eventsSinceSnapshot)
    {
        var ledger = new ShoppingLedger { Id = id, OwnerId = ownerId, Version = snapshotVersion };
        foreach (var item in snapshotItems)
            ledger.items[item.Key] = item.Value;

        foreach (var @event in eventsSinceSnapshot)
        {
            ledger.Apply(@event);
            ledger.Version++;
        }

        return ledger;
    }

    public void AddItem(string itemName, decimal quantity, string? unit, string source)
    {
        Ensure.That(itemName).IsNotNullOrWhiteSpace();
        Ensure.That(source).IsNotNullOrWhiteSpace();
        RaiseEvent(new ShoppingItemAdded(itemName, quantity, unit, source));
    }

    public void MarkBought(string itemName, decimal quantity, string? unit)
    {
        Ensure.That(itemName).IsNotNullOrWhiteSpace();
        RaiseEvent(new ShoppingItemBought(itemName, quantity, unit));
    }

    public void MarkConsumed(string itemName, decimal quantity, string? unit, string source)
    {
        Ensure.That(itemName).IsNotNullOrWhiteSpace();
        Ensure.That(source).IsNotNullOrWhiteSpace();
        RaiseEvent(new ShoppingItemConsumed(itemName, quantity, unit, source));
    }

    public void RemoveItem(string itemName, decimal quantity, string? unit, string? reason = null)
    {
        Ensure.That(itemName).IsNotNullOrWhiteSpace();
        RaiseEvent(new ShoppingItemRemoved(itemName, quantity, unit, reason));
    }

    public void AdjustQuantity(string itemName, decimal newQuantity, string? unit)
    {
        Ensure.That(itemName).IsNotNullOrWhiteSpace();
        RaiseEvent(new ShoppingItemQuantityAdjusted(itemName, newQuantity, unit));
    }

    public void UndoBought(string itemName, decimal quantity, string? unit)
    {
        Ensure.That(itemName).IsNotNullOrWhiteSpace();
        RaiseEvent(new ShoppingItemUndoBought(itemName, quantity, unit));
    }

    public void AddAsAtHome(string itemName, decimal quantity, string? unit)
    {
        Ensure.That(itemName).IsNotNullOrWhiteSpace();
        RaiseEvent(new ShoppingItemAddedAsAtHome(itemName, quantity, unit));
    }

    public void StartShoppingMode()
    {
        if (IsInShoppingMode) return;
        RaiseEvent(new ShoppingModeStarted(DateTime.UtcNow));
    }

    public void EndShoppingMode(bool acceptPendingChanges)
    {
        if (!IsInShoppingMode) return;
        RaiseEvent(new ShoppingModeEnded(acceptPendingChanges));
    }

    public void MarkCommitted()
    {
        uncommittedEvents.Clear();
    }

    private void RaiseEvent(IDomainEvent @event)
    {
        Apply(@event);
        uncommittedEvents.Add(@event);
        Version++;
    }

    private void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case ShoppingItemAdded e:
                GetOrCreateItem(e.ItemName, e.Unit).OnList += e.Quantity;
                if (IsInShoppingMode) PendingChangesCount++;
                break;
            case ShoppingItemBought e:
                GetOrCreateItem(e.ItemName, e.Unit).Bought += e.Quantity;
                break;
            case ShoppingItemConsumed e:
                GetOrCreateItem(e.ItemName, e.Unit).Consumed += e.Quantity;
                break;
            case ShoppingItemRemoved e:
                GetOrCreateItem(e.ItemName, e.Unit).Removed += e.Quantity;
                if (IsInShoppingMode) PendingChangesCount++;
                break;
            case ShoppingItemQuantityAdjusted e:
                GetOrCreateItem(e.ItemName, e.Unit).OnList = e.NewQuantity;
                if (IsInShoppingMode) PendingChangesCount++;
                break;
            case ShoppingItemUndoBought e:
                var undoItem = GetOrCreateItem(e.ItemName, e.Unit);
                undoItem.Bought = Math.Max(0, undoItem.Bought - e.Quantity);
                break;
            case ShoppingItemAddedAsAtHome e:
                var atHomeItem = GetOrCreateItem(e.ItemName, e.Unit);
                atHomeItem.Bought += e.Quantity;
                break;
            case ShoppingModeStarted e:
                IsInShoppingMode = true;
                ShoppingModeStartedAt = e.SnapshotTakenAt;
                PendingChangesCount = 0;
                break;
            case ShoppingModeEnded:
                IsInShoppingMode = false;
                ShoppingModeStartedAt = null;
                PendingChangesCount = 0;
                break;
        }
    }

    private ShoppingItemState GetOrCreateItem(string itemName, string? unit)
    {
        var key = $"{itemName.ToLowerInvariant()}|{unit ?? string.Empty}";
        if (!items.TryGetValue(key, out var item))
        {
            item = new ShoppingItemState { ItemName = itemName, Unit = unit };
            items[key] = item;
        }

        return item;
    }
}
