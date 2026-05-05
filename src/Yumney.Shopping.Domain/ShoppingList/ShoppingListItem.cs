using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Quantities;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

public sealed class ShoppingListItem : Entity<ShoppingListItemIdentifier>
{
	public ItemName Name { get; private set; } = default!;

	public Quantity? Quantity { get; private set; }

	public IngredientCategory Category { get; private set; } = IngredientCategory.Other;

	public bool IsChecked { get; private set; }

	private ShoppingListItem()
	{
	}

	public static ShoppingListItem Create(ItemName name, Quantity? quantity, IngredientCategory? category = null)
	{
		return new ShoppingListItem
		{
			Id = ShoppingListItemIdentifier.New(),
			Name = name,
			Quantity = quantity,
			Category = category ?? IngredientCategory.Other,
			IsChecked = false,
		};
	}

	internal static ShoppingListItem Hydrate(
		ShoppingListItemIdentifier id,
		ItemName name,
		Quantity? quantity,
		IngredientCategory? category = null)
	{
		return new ShoppingListItem
		{
			Id = id,
			Name = name,
			Quantity = quantity,
			Category = category ?? IngredientCategory.Other,
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

	internal ShoppingListItem ChangeCategoryTo(IngredientCategory category)
	{
		Category = category;
		return this;
	}
}
