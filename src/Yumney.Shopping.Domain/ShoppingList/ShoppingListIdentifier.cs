using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

public sealed record ShoppingListIdentifier : IValueObject
{
    public Guid Value { get; }

    private ShoppingListIdentifier(Guid value)
    {
        Value = Ensure.That(value).IsNotEmpty().AndReturn();
    }

    public static ShoppingListIdentifier New() => new(Guid.CreateVersion7());

    public static ShoppingListIdentifier From(Guid value) => new(value);

    public static ShoppingListIdentifier? FromNullable(Guid? value) =>
        value.HasValue ? new ShoppingListIdentifier(value.Value) : null;

    public override string ToString() => Value.ToString();
}
