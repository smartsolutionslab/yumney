using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;

public sealed class LedgerTransaction : Entity<LedgerTransactionIdentifier>
{
    public ItemName ItemName { get; private set; } = default!;

    public LedgerAction Action { get; private set; }

    public decimal Quantity { get; private set; }

    public string? Unit { get; private set; }

    public TransactionSource Source { get; private set; } = default!;

    public DateTime OccurredAt { get; private set; }

    private LedgerTransaction()
    {
    }

    internal static LedgerTransaction Create(
        ItemName itemName,
        LedgerAction action,
        decimal quantity,
        string? unit,
        TransactionSource source)
    {
        return new LedgerTransaction
        {
            Id = LedgerTransactionIdentifier.New(),
            ItemName = itemName,
            Action = action,
            Quantity = quantity,
            Unit = unit,
            Source = source,
            OccurredAt = DateTime.UtcNow,
        };
    }
}
