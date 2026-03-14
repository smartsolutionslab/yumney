using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

public sealed class ShoppingListItem : Entity<Guid>
{
    public ItemName Name { get; private set; } = default!;

    public Amount? Amount { get; private set; }

    public Unit? Unit { get; private set; }

    private ShoppingListItem()
    {
    }

    public static ShoppingListItem Create(ItemName name, Amount? amount, Unit? unit)
    {
        return new ShoppingListItem
        {
            Id = Guid.NewGuid(),
            Name = name,
            Amount = amount,
            Unit = unit,
        };
    }
}
