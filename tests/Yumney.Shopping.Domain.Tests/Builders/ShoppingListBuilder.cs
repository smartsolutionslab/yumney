using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using ShoppingListAggregate = SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.Tests.Builders;

public sealed class ShoppingListBuilder
{
	private readonly List<ShoppingListItem> items = [];
	private ShoppingListTitle title = ShoppingListTitle.From("Test Shopping List");
	private OwnerIdentifier owner = OwnerIdentifier.From("user-123");
	private RecipeReference? recipeReference;

	public ShoppingListBuilder WithTitle(string value) => WithTitle(ShoppingListTitle.From(value));

	public ShoppingListBuilder WithTitle(ShoppingListTitle value)
	{
		title = value;
		return this;
	}

	public ShoppingListBuilder OwnedBy(string ownerId) => OwnedBy(OwnerIdentifier.From(ownerId));

	public ShoppingListBuilder OwnedBy(OwnerIdentifier value)
	{
		owner = value;
		return this;
	}

	public ShoppingListBuilder WithItem(string name, decimal? amount = null, Unit? unit = null)
	{
		var quantity = Quantity.FromNullable(Amount.FromNullable(amount), unit);
		items.Add(ShoppingListItem.Create(ItemName.From(name), quantity));
		return this;
	}

	public ShoppingListBuilder WithItem(ShoppingListItem item)
	{
		items.Add(item);
		return this;
	}

	public ShoppingListBuilder FromRecipe(Guid recipeIdentifier)
	{
		recipeReference = RecipeReference.From(recipeIdentifier);
		return this;
	}

	public ShoppingListAggregate Build()
	{
		if (items.Count == 0)
		{
			items.Add(ShoppingListItem.Create(ItemName.From("Default Item"), null));
		}

		return ShoppingListAggregate.Create(title, owner, items, recipeReference);
	}
}
