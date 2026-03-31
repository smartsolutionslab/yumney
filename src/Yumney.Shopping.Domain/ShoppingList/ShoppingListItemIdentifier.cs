using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

public sealed record ShoppingListItemIdentifier : GuidIdentifier
{
    private ShoppingListItemIdentifier(Guid value)
        : base(value)
    {
    }

    public static ShoppingListItemIdentifier New() => new(Guid.NewGuid());

    public static ShoppingListItemIdentifier From(Guid value) => new(value);
}
