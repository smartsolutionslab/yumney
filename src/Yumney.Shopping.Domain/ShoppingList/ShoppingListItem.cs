using SmartSolutionsLab.Yumney.Shared.Abstractions;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

public sealed class ShoppingListItem : Entity<ShoppingListItemIdentifier>
{
	public ItemName Name { get; private set; } = default!;

	public Quantity? Quantity { get; private set; }

	public bool IsChecked { get; private set; }

	private ShoppingListItem()
	{
	}

	public static ShoppingListItem Create(ItemName name, Quantity? quantity)
	{
		return new ShoppingListItem
		{
			Id = ShoppingListItemIdentifier.New(),
			Name = name,
			Quantity = quantity,
			IsChecked = false,
		};
	}

	internal static ShoppingListItem Hydrate(ShoppingListItemIdentifier id, ItemName name, Quantity? quantity)
	{
		return new ShoppingListItem
		{
			Id = id,
			Name = name,
			Quantity = quantity,
			IsChecked = false,
		};
	}

	internal ShoppingListItem MarkChecked()
	{
		IsChecked = true;
		return this;
	}

	internal ShoppingListItem MarkUnchecked()
	{
		IsChecked = false;
		return this;
	}
}
