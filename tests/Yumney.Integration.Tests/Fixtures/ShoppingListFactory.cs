using System;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;

public static class ShoppingListFactory
{
	public static ShoppingList WeeklyGroceries(string? owner = null) => ShoppingList.Create(
		ShoppingListTitle.From("Weekly Groceries"),
		OwnerIdentifier.From(owner ?? "integration-test-user"),
		[
			ShoppingListItem.Create(ItemName.From("Milk"), Quantity.Of(Amount.From(2), Unit.Liter)),
			ShoppingListItem.Create(ItemName.From("Bread"), Quantity.Of(Amount.From(1), null)),
			ShoppingListItem.Create(ItemName.From("Eggs"), Quantity.Of(Amount.From(12), null)),
			ShoppingListItem.Create(ItemName.From("Butter"), Quantity.Of(Amount.From(250), Unit.Gram))
		]);

	public static ShoppingList PartySupplies(string? owner = null) => ShoppingList.Create(
		ShoppingListTitle.From("Party Supplies"),
		OwnerIdentifier.From(owner ?? "integration-test-user"),
		[
			ShoppingListItem.Create(ItemName.From("Chips"), Quantity.Of(Amount.From(3), Unit.Bag)),
			ShoppingListItem.Create(ItemName.From("Soda"), Quantity.Of(Amount.From(6), Unit.Bottle)),
			ShoppingListItem.Create(ItemName.From("Napkins"), Quantity.Of(Amount.From(1), Unit.Pack))
		]);

	public static ShoppingList BakingIngredients(string? owner = null) => ShoppingList.Create(
		ShoppingListTitle.From("Baking Ingredients"),
		OwnerIdentifier.From(owner ?? "integration-test-user"),
		[
			ShoppingListItem.Create(ItemName.From("Flour"), Quantity.Of(Amount.From(1000), Unit.Gram)),
			ShoppingListItem.Create(ItemName.From("Sugar"), Quantity.Of(Amount.From(500), Unit.Gram))
		],
		RecipeReference.New());
}
