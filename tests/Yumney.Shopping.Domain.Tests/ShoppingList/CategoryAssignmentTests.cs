using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Quantities;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.Tests.ShoppingList;

public class CategoryAssignmentTests
{
	[Fact]
	public void Create_NoCategorySupplied_DefaultsToOther()
	{
		var item = ShoppingListItem.Create(
			ItemName.From("Mystery"),
			Quantity.Of(Amount.From(1), Unit.Piece));

		item.Category.Should().Be(IngredientCategory.Other);
	}

	[Fact]
	public void Create_WithCategory_StoresIt()
	{
		var item = ShoppingListItem.Create(
			ItemName.From("Milk"),
			Quantity.Of(Amount.From(1), Unit.Liter),
			IngredientCategory.Dairy);

		item.Category.Should().Be(IngredientCategory.Dairy);
	}

	[Fact]
	public void Create_PropagatesCategoryIntoListItemAddedEvent()
	{
		var item = ShoppingListItem.Create(
			ItemName.From("Cheese"),
			Quantity.Of(Amount.From(200), Unit.Gram),
			IngredientCategory.Dairy);

		var list = Domain.ShoppingList.ShoppingList.Create(
			ShoppingListTitle.From("Cheese list"),
			OwnerIdentifier.From("user-123"),
			[item]);

		var added = list.UncommittedEvents.OfType<ListItemAdded>().Single();
		added.Category.Should().Be(IngredientCategory.Dairy);
	}

	[Fact]
	public void ChangeItemCategory_RaisesListItemCategoryChanged()
	{
		var item = ShoppingListItem.Create(ItemName.From("Salmon"), null, IngredientCategory.Other);
		var list = Domain.ShoppingList.ShoppingList.Create(
			ShoppingListTitle.From("Dinner"),
			OwnerIdentifier.From("user-123"),
			[item]);
		list.MarkCommitted();

		list.ChangeItemCategory(item.Id, IngredientCategory.MeatFish);

		var raised = list.UncommittedEvents.OfType<ListItemCategoryChanged>().Single();
		raised.ItemId.Should().Be(item.Id);
		raised.Category.Should().Be(IngredientCategory.MeatFish);
	}

	[Fact]
	public void ChangeItemCategory_UpdatesItemState()
	{
		var item = ShoppingListItem.Create(ItemName.From("Bread"), null, IngredientCategory.Other);
		var list = Domain.ShoppingList.ShoppingList.Create(
			ShoppingListTitle.From("Lunch"),
			OwnerIdentifier.From("user-123"),
			[item]);

		list.ChangeItemCategory(item.Id, IngredientCategory.Bakery);

		list.Items.Single(candidate => candidate.Id == item.Id).Category.Should().Be(IngredientCategory.Bakery);
	}

	[Fact]
	public void Replay_CategoryEventsRehydrateLatestState()
	{
		var item = ShoppingListItem.Create(ItemName.From("Apple"), null, IngredientCategory.Other);
		var original = Domain.ShoppingList.ShoppingList.Create(
			ShoppingListTitle.From("Snacks"),
			OwnerIdentifier.From("user-123"),
			[item]);
		original.ChangeItemCategory(item.Id, IngredientCategory.Produce);

		var replayed = Domain.ShoppingList.ShoppingList.FromEvents(original.Identifier, original.UncommittedEvents);

		replayed.Items.Single(candidate => candidate.Id == item.Id).Category.Should().Be(IngredientCategory.Produce);
	}
}
