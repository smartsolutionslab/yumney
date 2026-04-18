using SmartSolutionsLab.Yumney.Shared.Common;

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

	public ShoppingListItem Check()
	{
		IsChecked = true;
		return this;
	}

	public ShoppingListItem Uncheck()
	{
		IsChecked = false;
		return this;
	}
}
