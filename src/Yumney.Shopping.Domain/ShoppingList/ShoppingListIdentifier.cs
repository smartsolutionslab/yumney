using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

public sealed record ShoppingListIdentifier : GuidIdentifier
{
    private ShoppingListIdentifier(Guid value)
        : base(value)
    {
    }

    public static ShoppingListIdentifier New() => new(Guid.NewGuid());

    public static ShoppingListIdentifier From(Guid value) => new(value);

    public static ShoppingListIdentifier? FromNullable(Guid? value) =>
        value.HasValue ? new ShoppingListIdentifier(value.Value) : null;
}
