using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

public sealed record ShoppingListIdentifier
{
    public Guid Value { get; }

    public ShoppingListIdentifier(Guid value)
    {
        Value = Ensure.That(value).IsNotEmpty().AndReturn();
    }

    public static ShoppingListIdentifier New() => new(Guid.NewGuid());

    public static ShoppingListIdentifier From(Guid value) => new(value);

    public static ShoppingListIdentifier? FromNullable(Guid? value) =>
        value.HasValue ? new ShoppingListIdentifier(value.Value) : null;

    public override string ToString() => Value.ToString();
}
