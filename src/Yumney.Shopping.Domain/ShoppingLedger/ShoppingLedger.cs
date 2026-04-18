using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger.Events;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;

public sealed class ShoppingLedger : EventSourcedAggregate<ShoppingLedgerIdentifier>
{
	private readonly Dictionary<string, ShoppingItemState> items = new(StringComparer.OrdinalIgnoreCase);

	public OwnerIdentifier OwnerId { get; private set; } = default!;

	public IReadOnlyDictionary<string, ShoppingItemState> Items => items;

	public bool IsInShoppingMode { get; private set; }

	public DateTime? ShoppingModeStartedAt { get; private set; }

	public int PendingChangesCount { get; private set; }

	private ShoppingLedger()
	{
		On<ShoppingItemAdded>(OnItemAdded);
		On<ShoppingItemBought>(OnItemBought);
		On<ShoppingItemConsumed>(OnItemConsumed);
		On<ShoppingItemRemoved>(OnItemRemoved);
		On<ShoppingItemQuantityAdjusted>(OnQuantityAdjusted);
		On<ShoppingItemUndoBought>(OnUndoBought);
		On<ShoppingItemAddedAsAtHome>(OnAddedAsAtHome);
		On<ShoppingModeStarted>(OnShoppingModeStarted);
		On<ShoppingModeEnded>(_ => OnShoppingModeEnded());
	}

	public static ShoppingLedger Create(OwnerIdentifier ownerId)
	{
		return new ShoppingLedger
		{
			Identifier = ShoppingLedgerIdentifier.CreateNew(),
			OwnerId = ownerId,
		};
	}

	public static ShoppingLedger FromEvents(
		ShoppingLedgerIdentifier identifier,
		OwnerIdentifier ownerId,
		IEnumerable<IDomainEvent> events,
		int startVersion = 0)
	{
		var ledger = new ShoppingLedger { Identifier = identifier, OwnerId = ownerId };
		ledger.LoadFromHistory(events, startVersion);

		return ledger;
	}

	public static ShoppingLedger FromSnapshot(
		ShoppingLedgerIdentifier identifier,
		OwnerIdentifier ownerId,
		Dictionary<string, ShoppingItemState> snapshotItems,
		int snapshotVersion,
		IEnumerable<IDomainEvent> eventsSinceSnapshot)
	{
		var ledger = new ShoppingLedger { Identifier = identifier, OwnerId = ownerId };
		foreach (var item in snapshotItems)
		{
			ledger.items[item.Key] = item.Value;
		}

		ledger.LoadFromHistory(eventsSinceSnapshot, snapshotVersion);

		return ledger;
	}

	public ShoppingLedger AddItem(ItemName itemName, Quantity quantity, ItemSource source)
	{
		RaiseEvent(new ShoppingItemAdded(itemName, quantity, source));
		return this;
	}

	public ShoppingLedger MarkBought(ItemName itemName, Quantity quantity)
	{
		RaiseEvent(new ShoppingItemBought(itemName, quantity));
		return this;
	}

	public ShoppingLedger MarkConsumed(ItemName itemName, Quantity quantity, ItemSource source)
	{
		RaiseEvent(new ShoppingItemConsumed(itemName, quantity, source));
		return this;
	}

	public ShoppingLedger RemoveItem(ItemName itemName, Quantity quantity, RemovalReason? reason = null)
	{
		RaiseEvent(new ShoppingItemRemoved(itemName, quantity, reason));
		return this;
	}

	public ShoppingLedger AdjustQuantity(ItemName itemName, Quantity newQuantity)
	{
		RaiseEvent(new ShoppingItemQuantityAdjusted(itemName, newQuantity));
		return this;
	}

	public ShoppingLedger UndoBought(ItemName itemName, Quantity quantity)
	{
		RaiseEvent(new ShoppingItemUndoBought(itemName, quantity));
		return this;
	}

	public ShoppingLedger AddAsAtHome(ItemName itemName, Quantity quantity)
	{
		RaiseEvent(new ShoppingItemAddedAsAtHome(itemName, quantity));
		return this;
	}

	public ShoppingLedger StartShoppingMode()
	{
		if (IsInShoppingMode) return this;
		RaiseEvent(new ShoppingModeStarted(DateTime.UtcNow));
		return this;
	}

	public ShoppingLedger EndShoppingMode(bool acceptPendingChanges)
	{
		if (!IsInShoppingMode) return this;
		RaiseEvent(new ShoppingModeEnded(acceptPendingChanges));
		return this;
	}

	private void OnItemAdded(ShoppingItemAdded e)
	{
		UpdateItem(e.ItemName, e.Quantity.Unit, s => s with { OnList = Amount.From(s.OnList + e.Quantity.Amount) });
		if (IsInShoppingMode)
		{
			PendingChangesCount++;
		}
	}

	private void OnItemBought(ShoppingItemBought e) =>
		UpdateItem(e.ItemName, e.Quantity.Unit, s => s with { Bought = Amount.From(s.Bought + e.Quantity.Amount) });

	private void OnItemConsumed(ShoppingItemConsumed e) =>
		UpdateItem(e.ItemName, e.Quantity.Unit, s => s with { Consumed = Amount.From(s.Consumed + e.Quantity.Amount) });

	private void OnItemRemoved(ShoppingItemRemoved e)
	{
		UpdateItem(e.ItemName, e.Quantity.Unit, s => s with { Removed = Amount.From(s.Removed + e.Quantity.Amount) });
		if (IsInShoppingMode)
		{
			PendingChangesCount++;
		}
	}

	private void OnQuantityAdjusted(ShoppingItemQuantityAdjusted e)
	{
		UpdateItem(e.ItemName, e.NewQuantity.Unit, s => s with { OnList = e.NewQuantity.Amount });
		if (IsInShoppingMode)
		{
			PendingChangesCount++;
		}
	}

	private void OnUndoBought(ShoppingItemUndoBought e) =>
		UpdateItem(e.ItemName, e.Quantity.Unit, s => s with { Bought = Amount.From(Math.Max(0, s.Bought - e.Quantity.Amount)) });

	private void OnAddedAsAtHome(ShoppingItemAddedAsAtHome e) =>
		UpdateItem(e.ItemName, e.Quantity.Unit, s => s with { Bought = Amount.From(s.Bought + e.Quantity.Amount) });

	private void OnShoppingModeStarted(ShoppingModeStarted e)
	{
		IsInShoppingMode = true;
		ShoppingModeStartedAt = e.SnapshotTakenAt;
		PendingChangesCount = 0;
	}

	private void OnShoppingModeEnded()
	{
		IsInShoppingMode = false;
		ShoppingModeStartedAt = null;
		PendingChangesCount = 0;
	}

#pragma warning disable SA1204
	private static string KeyFor(ItemName itemName, Unit? unit) =>
		$"{itemName.Value.ToLowerInvariant()}|{unit?.Value ?? string.Empty}";

	private void UpdateItem(ItemName itemName, Unit? unit, Func<ShoppingItemState, ShoppingItemState> update)
	{
		var key = KeyFor(itemName, unit);
		if (!items.TryGetValue(key, out var current))
		{
			current = new ShoppingItemState { ItemName = itemName, Unit = unit };
		}

		items[key] = update(current);
	}
}
