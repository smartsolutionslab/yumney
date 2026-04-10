using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;

public sealed record LedgerTransactionIdentifier : IValueObject
{
    public Guid Value { get; }

    private LedgerTransactionIdentifier(Guid value)
    {
        Value = Ensure.That(value).IsNotEmpty().AndReturn();
    }

    public static LedgerTransactionIdentifier New() => new(Guid.CreateVersion7());

    public static LedgerTransactionIdentifier From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
