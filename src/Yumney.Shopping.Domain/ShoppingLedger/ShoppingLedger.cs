using SmartSolutionsLab.Yumney.Shared.Common;
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
        RaiseEvent(new ShoppingItemAdded(itemName, quantity, unit, source));
    }

    public void MarkBought(string itemName, decimal quantity, string? unit)
    {
        RaiseEvent(new ShoppingItemBought(itemName, quantity, unit));
    }

    public void MarkConsumed(string itemName, decimal quantity, string? unit, string source)
    {
        RaiseEvent(new ShoppingItemConsumed(itemName, quantity, unit, source));
    }

    public void RemoveItem(string itemName, decimal quantity, string? unit, string? reason = null)
    {
        RaiseEvent(new ShoppingItemRemoved(itemName, quantity, unit, reason));
    }

    public void AdjustQuantity(string itemName, decimal newQuantity, string? unit)
    {
        RaiseEvent(new ShoppingItemQuantityAdjusted(itemName, newQuantity, unit));
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
                break;
            case ShoppingItemBought e:
                GetOrCreateItem(e.ItemName, e.Unit).Bought += e.Quantity;
                break;
            case ShoppingItemConsumed e:
                GetOrCreateItem(e.ItemName, e.Unit).Consumed += e.Quantity;
                break;
            case ShoppingItemRemoved e:
                GetOrCreateItem(e.ItemName, e.Unit).Removed += e.Quantity;
                break;
            case ShoppingItemQuantityAdjusted e:
                GetOrCreateItem(e.ItemName, e.Unit).OnList = e.NewQuantity;
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
