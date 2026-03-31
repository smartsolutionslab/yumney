using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

public sealed class ShoppingListItem : Entity<ShoppingListItemIdentifier>
{
    public ItemName Name { get; private set; } = default!;

    public Amount? Amount { get; private set; }

    public Unit? Unit { get; private set; }

    public bool IsChecked { get; private set; }

    private ShoppingListItem()
    {
    }

    public static ShoppingListItem Create(ItemName name, Amount? amount, Unit? unit)
    {
        return new ShoppingListItem
        {
            Id = ShoppingListItemIdentifier.New(),
            Name = name,
            Amount = amount,
            Unit = unit,
            IsChecked = false,
        };
    }

    public void Check()
    {
        IsChecked = true;
    }

    public void Uncheck()
    {
        IsChecked = false;
    }
}
