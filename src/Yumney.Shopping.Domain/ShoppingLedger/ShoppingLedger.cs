using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;

public sealed class ShoppingLedger : AggregateRoot<ShoppingLedgerIdentifier>
{
    private readonly List<LedgerTransaction> transactions = [];

    public OwnerIdentifier Owner { get; private set; } = default!;

    public IReadOnlyList<LedgerTransaction> Transactions => transactions.AsReadOnly();

    private ShoppingLedger()
    {
    }

    public static ShoppingLedger Create(OwnerIdentifier owner)
    {
        return new ShoppingLedger
        {
            Id = ShoppingLedgerIdentifier.New(),
            Owner = owner,
        };
    }

    public LedgerTransaction AddItem(ItemName itemName, decimal quantity, string? unit, TransactionSource source)
    {
        var transaction = LedgerTransaction.Create(itemName, LedgerAction.Added, quantity, unit, source);
        transactions.Add(transaction);
        return transaction;
    }

    public LedgerTransaction MarkBought(ItemName itemName, decimal quantity, string? unit)
    {
        var transaction = LedgerTransaction.Create(itemName, LedgerAction.Bought, quantity, unit, TransactionSource.Manual);
        transactions.Add(transaction);
        return transaction;
    }

    public LedgerTransaction MarkConsumed(ItemName itemName, decimal quantity, string? unit, TransactionSource source)
    {
        var transaction = LedgerTransaction.Create(itemName, LedgerAction.Consumed, quantity, unit, source);
        transactions.Add(transaction);
        return transaction;
    }

    public LedgerTransaction RemoveItem(ItemName itemName, decimal quantity, string? unit, string? reason = null)
    {
        var source = reason is not null ? TransactionSource.From($"removed:{reason}") : TransactionSource.Manual;
        var transaction = LedgerTransaction.Create(itemName, LedgerAction.Removed, quantity, unit, source);
        transactions.Add(transaction);
        return transaction;
    }

    public LedgerTransaction AdjustQuantity(ItemName itemName, decimal newQuantity, string? unit)
    {
        var transaction = LedgerTransaction.Create(itemName, LedgerAction.Adjusted, newQuantity, unit, TransactionSource.Manual);
        transactions.Add(transaction);
        return transaction;
    }

    public LedgerTransaction Rollback(LedgerTransactionIdentifier transactionId)
    {
        var original = transactions.FirstOrDefault(t => t.Id == transactionId)
            ?? throw new EntityNotFoundException(nameof(LedgerTransaction), transactionId.Value);

        var transaction = LedgerTransaction.Create(
            original.ItemName,
            LedgerAction.RolledBack,
            original.Quantity,
            original.Unit,
            TransactionSource.From($"rollback:{transactionId.Value}"));
        transactions.Add(transaction);
        return transaction;
    }

    public IReadOnlyList<ItemBalance> CalculateBalance()
    {
        var balances = new Dictionary<string, (decimal OnList, decimal Bought, decimal Consumed, decimal Removed)>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var tx in transactions)
        {
            var key = tx.ItemName.Value;
            if (!balances.ContainsKey(key))
                balances[key] = (0, 0, 0, 0);

            var current = balances[key];
            balances[key] = tx.Action switch
            {
                LedgerAction.Added => (current.OnList + tx.Quantity, current.Bought, current.Consumed, current.Removed),
                LedgerAction.Bought => (current.OnList, current.Bought + tx.Quantity, current.Consumed, current.Removed),
                LedgerAction.Consumed => (current.OnList, current.Bought, current.Consumed + tx.Quantity, current.Removed),
                LedgerAction.Removed => (current.OnList, current.Bought, current.Consumed, current.Removed + tx.Quantity),
                LedgerAction.Adjusted => (tx.Quantity, current.Bought, current.Consumed, current.Removed),
                LedgerAction.RolledBack => ReverseTransaction(current, tx, key),
                _ => current,
            };
        }

        return balances.Select(kvp =>
        {
            var (onList, bought, consumed, removed) = kvp.Value;
            var atHome = bought - consumed - removed;
            return new ItemBalance(kvp.Key, onList, bought, consumed, Math.Max(0, atHome));
        }).ToList();
    }

    /// <summary>
    /// Project transactions into merged shopping items — same item + same unit
    /// summed into one line with source breakdown.
    /// Different units for the same item produce separate lines.
    /// </summary>
    /// <returns>Merged items grouped by item name and unit.</returns>
    public IReadOnlyList<MergedShoppingItem> GetMergedItems()
    {
        var groups = new Dictionary<string, List<LedgerTransaction>>(StringComparer.OrdinalIgnoreCase);

        foreach (var tx in transactions.Where(t => t.Action == LedgerAction.Added))
        {
            var key = $"{tx.ItemName.Value}|{tx.Unit ?? string.Empty}";
            if (!groups.ContainsKey(key))
                groups[key] = [];
            groups[key].Add(tx);
        }

        var boughtKeys = transactions
            .Where(t => t.Action == LedgerAction.Bought)
            .Select(t => $"{t.ItemName.Value}|{t.Unit ?? string.Empty}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return groups.Select(kvp =>
        {
            var txList = kvp.Value;
            var first = txList[0];
            var totalQuantity = txList.Sum(t => t.Quantity);
            var sources = txList.Select(t => new ItemSource(t.Quantity, t.Source.Value, t.OccurredAt)).ToList();
            var isBought = boughtKeys.Contains(kvp.Key);

            return new MergedShoppingItem(first.ItemName.Value, totalQuantity, first.Unit, isBought, sources);
        }).ToList();
    }

    private (decimal OnList, decimal Bought, decimal Consumed, decimal Removed) ReverseTransaction(
        (decimal OnList, decimal Bought, decimal Consumed, decimal Removed) current,
        LedgerTransaction rollbackTx,
        string key)
    {
        var originalId = rollbackTx.Source.Value.Replace("rollback:", string.Empty);
        if (!Guid.TryParse(originalId, out var guid))
            return current;

        var original = transactions.FirstOrDefault(t => t.Id == LedgerTransactionIdentifier.From(guid));
        if (original is null)
            return current;

        return original.Action switch
        {
            LedgerAction.Added => (current.OnList - rollbackTx.Quantity, current.Bought, current.Consumed, current.Removed),
            LedgerAction.Bought => (current.OnList, current.Bought - rollbackTx.Quantity, current.Consumed, current.Removed),
            LedgerAction.Consumed => (current.OnList, current.Bought, current.Consumed - rollbackTx.Quantity, current.Removed),
            LedgerAction.Removed => (current.OnList, current.Bought, current.Consumed, current.Removed - rollbackTx.Quantity),
            _ => current,
        };
    }
}
