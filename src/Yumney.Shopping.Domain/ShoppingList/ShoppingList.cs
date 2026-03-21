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

    public static ShoppingList Create(ShoppingListTitle title, OwnerIdentifier owner, IReadOnlyList<ShoppingListItem> items, RecipeReference? recipeReference = null)
    {
        Ensure.That((IReadOnlyCollection<ShoppingListItem>)items).IsNotEmpty();

        var shoppingList = new ShoppingList
        {
            Id = new ShoppingListIdentifier(Guid.NewGuid()),
            Title = title,
            Owner = owner,
            RecipeReference = recipeReference,
            CreatedAt = DateTime.UtcNow,
        };

        shoppingList.items.AddRange(items);

        shoppingList.AddDomainEvent(new ShoppingListCreatedEvent(shoppingList.Id, title));

        return shoppingList;
    }
}
