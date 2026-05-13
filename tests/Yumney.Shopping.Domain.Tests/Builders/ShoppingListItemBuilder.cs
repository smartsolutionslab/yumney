using SmartSolutionsLab.Yumney.Shared.Quantities;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.Tests.Builders;

/// <summary>
/// Test builder for the <see cref="ShoppingListItem"/> entity. Defaults to
/// a sensible name and no quantity / Other category. Use the explicit
/// <c>WithQuantity</c> / <c>InCategory</c> methods to vary individually.
/// </summary>
public sealed class ShoppingListItemBuilder
{
	private ItemName name = ItemName.From("Test Item");
	private Quantity? quantity;
	private IngredientCategory? category;

	public static ShoppingListItemBuilder A() => new();

	public ShoppingListItemBuilder Named(string value)
	{
		name = ItemName.From(value);
		return this;
	}

	public ShoppingListItemBuilder Named(ItemName value)
	{
		name = value;
		return this;
	}

	public ShoppingListItemBuilder WithQuantity(decimal amount, Unit? unit = null)
	{
		quantity = Quantity.Of(Amount.From(amount), unit);
		return this;
	}

	public ShoppingListItemBuilder WithQuantity(Quantity value)
	{
		quantity = value;
		return this;
	}

	public ShoppingListItemBuilder WithoutQuantity()
	{
		quantity = null;
		return this;
	}

	public ShoppingListItemBuilder InCategory(IngredientCategory value)
	{
		category = value;
		return this;
	}

	public ShoppingListItem Build() => ShoppingListItem.Create(name, quantity, category);

	public static implicit operator ShoppingListItem(ShoppingListItemBuilder builder) => builder.Build();
}
