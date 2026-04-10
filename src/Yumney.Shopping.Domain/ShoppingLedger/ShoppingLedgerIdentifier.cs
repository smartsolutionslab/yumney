using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;

public sealed record ShoppingLedgerIdentifier : IValueObject
{
    public Guid Value { get; }

    private ShoppingLedgerIdentifier(Guid value)
    {
        Value = Ensure.That(value).IsNotEmpty().AndReturn();
    }

    public static ShoppingLedgerIdentifier New() => new(Guid.CreateVersion7());

    public static ShoppingLedgerIdentifier From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
