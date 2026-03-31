using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

public sealed class ShoppingList : AggregateRoot<ShoppingListIdentifier>
{
    private readonly List<ShoppingListItem> items = [];

    public ShoppingListTitle Title { get; private set; } = default!;

    public OwnerIdentifier Owner { get; private set; } = default!;

    public RecipeReference? RecipeReference { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public IReadOnlyList<ShoppingListItem> Items => items.AsReadOnly();

    private ShoppingList()
    {
    }

    public static ShoppingList Create(
        ShoppingListTitle title,
        OwnerIdentifier owner,
        IReadOnlyList<ShoppingListItem> items,
        RecipeReference? recipeReference = null)
    {
        Ensure.That(items).IsNotEmpty();

        var shoppingList = new ShoppingList
        {
            Id = ShoppingListIdentifier.New(),
            Title = title,
            Owner = owner,
            RecipeReference = recipeReference,
            CreatedAt = DateTime.UtcNow,
        };

        shoppingList.items.AddRange(items);

        shoppingList.AddDomainEvent(new ShoppingListCreatedEvent(shoppingList.Id, title));

        return shoppingList;
    }

    public void CheckOffItem(ShoppingListItemIdentifier itemId)
    {
        var item = items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new GuardException(nameof(itemId), $"Item {itemId} not found in shopping list.");
        item.Check();
    }

    public void UncheckItem(ShoppingListItemIdentifier itemId)
    {
        var item = items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new GuardException(nameof(itemId), $"Item {itemId} not found in shopping list.");
        item.Uncheck();
    }

    public void CheckAllItems()
    {
        foreach (var item in items)
        {
            item.Check();
        }
    }

    public void UncheckAllItems()
    {
        foreach (var item in items)
        {
            item.Uncheck();
        }
    }
}
