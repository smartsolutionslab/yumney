using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

public sealed class ShoppingList : AggregateRoot<Guid>
{
    private readonly List<ShoppingListItem> items = [];

    public ShoppingListTitle Title { get; private set; } = default!;

    public OwnerIdentifier Owner { get; private set; } = default!;

    public Guid? RecipeIdentifier { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public IReadOnlyList<ShoppingListItem> Items => items.AsReadOnly();

    private ShoppingList()
    {
    }

    public static ShoppingList Create(ShoppingListTitle title, OwnerIdentifier owner, IReadOnlyList<ShoppingListItem> items, Guid? recipeIdentifier = null)
    {
        Ensure.That((IReadOnlyCollection<ShoppingListItem>)items).IsNotEmpty();

        var shoppingList = new ShoppingList
        {
            Id = Guid.NewGuid(),
            Title = title,
            Owner = owner,
            RecipeIdentifier = recipeIdentifier,
            CreatedAt = DateTime.UtcNow,
        };

        shoppingList.items.AddRange(items);

        shoppingList.AddDomainEvent(new ShoppingListCreatedEvent(new ShoppingListIdentifier(shoppingList.Id), title));

        return shoppingList;
    }
}
