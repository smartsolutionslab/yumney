using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.Tests.ShoppingList;

public class ShoppingListTests
{
	[Fact]
	public void Create_ValidInput_CreatesShoppingListWithIdentifier()
	{
		var shoppingList = CreateValidShoppingList();

		shoppingList.Identifier.Should().NotBeNull();
	}

	[Fact]
	public void Create_ValidInput_SetsTitle()
	{
		var title = ShoppingListTitle.From("Weekly Groceries");

		var shoppingList = CreateValidShoppingList(title: title);

		shoppingList.Title.Should().Be(title);
	}

	[Fact]
	public void Create_ValidInput_SetsOwner()
	{
		var owner = OwnerIdentifier.From("user-123");

		var shoppingList = CreateValidShoppingList(owner: owner);

		shoppingList.Owner.Should().Be(owner);
	}

	[Fact]
	public void Create_ValidInput_SetsCreatedAtCloseToNow()
	{
		var before = DateTime.UtcNow;

		var shoppingList = CreateValidShoppingList();

		shoppingList.CreatedAt.Should().BeCloseTo(before, TimeSpan.FromSeconds(5));
	}

	[Fact]
	public void Create_ValidInput_SetsItems()
	{
		List<ShoppingListItem> items =
		[
			ShoppingListItem.Create(ItemName.From("Flour"), Quantity.Of(Amount.From(500), Unit.Gram)),
			ShoppingListItem.Create(ItemName.From("Sugar"), Quantity.Of(Amount.From(200), Unit.Gram))
		];

		var shoppingList = CreateValidShoppingList(items: items);

		shoppingList.Items.Should().HaveCount(2);
	}

	[Fact]
	public void Create_WithRecipeReference_SetsRecipeReference()
	{
		var recipeReference = RecipeReference.New();

		var shoppingList = CreateValidShoppingList(recipeReference: recipeReference);

		shoppingList.RecipeReference.Should().Be(recipeReference);
	}

	[Fact]
	public void Create_WithoutRecipeReference_RecipeReferenceIsNull()
	{
		var shoppingList = CreateValidShoppingList();

		shoppingList.RecipeReference.Should().BeNull();
	}

	[Fact]
	public void Create_EmptyItems_ThrowsGuardException()
	{
		var act = () => CreateValidShoppingList(items: []);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void Create_RaisesShoppingListCreatedEvent()
	{
		var title = ShoppingListTitle.From("Groceries");

		var shoppingList = CreateValidShoppingList(title: title);

		shoppingList.UncommittedEvents.OfType<ShoppingListCreated>().Should().ContainSingle()
			.Which.Title.Should().Be(title);
	}

	[Fact]
	public void Create_ShoppingListCreatedEvent_ContainsShoppingListIdentifier()
	{
		var shoppingList = CreateValidShoppingList();

		var domainEvent = shoppingList.UncommittedEvents.OfType<ShoppingListCreated>().Should().ContainSingle().Subject;

		domainEvent.Identifier.Should().Be(shoppingList.Identifier);
	}

	[Fact]
	public void Create_RaisesListItemAddedEventPerItem()
	{
		List<ShoppingListItem> items =
		[
			ShoppingListItem.Create(ItemName.From("Flour"), Quantity.Of(Amount.From(500), Unit.Gram)),
			ShoppingListItem.Create(ItemName.From("Sugar"), Quantity.Of(Amount.From(200), Unit.Gram))
		];

		var shoppingList = CreateValidShoppingList(items: items);

		shoppingList.UncommittedEvents.OfType<ListItemAdded>().Should().HaveCount(2);
	}

	[Fact]
	public void Create_GeneratesUniqueIdentifiers()
	{
		var list1 = CreateValidShoppingList();
		var list2 = CreateValidShoppingList();

		list1.Identifier.Should().NotBe(list2.Identifier);
	}

	[Fact]
	public void CheckOffItem_ChecksTheItem()
	{
		List<ShoppingListItem> items =
		[
			ShoppingListItem.Create(ItemName.From("Flour"), Quantity.Of(Amount.From(500), Unit.Gram))
		];
		var shoppingList = CreateValidShoppingList(items: items);

		shoppingList.CheckOffItem(items[0].Id);

		shoppingList.Items[0].IsChecked.Should().BeTrue();
	}

	[Fact]
	public void CheckOffItem_RaisesListItemCheckedEvent()
	{
		List<ShoppingListItem> items =
		[
			ShoppingListItem.Create(ItemName.From("Flour"), Quantity.Of(Amount.From(500), Unit.Gram))
		];
		var shoppingList = CreateValidShoppingList(items: items);

		shoppingList.CheckOffItem(items[0].Id);

		shoppingList.UncommittedEvents.OfType<ListItemChecked>().Should().ContainSingle()
			.Which.ItemId.Should().Be(items[0].Id);
	}

	[Fact]
	public void UncheckItem_UnchecksTheItem()
	{
		List<ShoppingListItem> items =
		[
			ShoppingListItem.Create(ItemName.From("Flour"), Quantity.Of(Amount.From(500), Unit.Gram))
		];
		var shoppingList = CreateValidShoppingList(items: items);
		shoppingList.CheckOffItem(items[0].Id);

		shoppingList.UncheckItem(items[0].Id);

		shoppingList.Items[0].IsChecked.Should().BeFalse();
	}

	[Fact]
	public void UncheckItem_RaisesListItemUncheckedEvent()
	{
		List<ShoppingListItem> items =
		[
			ShoppingListItem.Create(ItemName.From("Flour"), Quantity.Of(Amount.From(500), Unit.Gram))
		];
		var shoppingList = CreateValidShoppingList(items: items);

		shoppingList.UncheckItem(items[0].Id);

		shoppingList.UncommittedEvents.OfType<ListItemUnchecked>().Should().ContainSingle()
			.Which.ItemId.Should().Be(items[0].Id);
	}

	[Fact]
	public void CheckAllItems_ChecksAllItems()
	{
		List<ShoppingListItem> items =
		[
			ShoppingListItem.Create(ItemName.From("Flour"), Quantity.Of(Amount.From(500), Unit.Gram)),
			ShoppingListItem.Create(ItemName.From("Sugar"), Quantity.Of(Amount.From(200), Unit.Gram))
		];
		var shoppingList = CreateValidShoppingList(items: items);

		shoppingList.CheckAllItems();

		shoppingList.Items.Should().OnlyContain(i => i.IsChecked);
	}

	[Fact]
	public void CheckAllItems_RaisesAllItemsCheckedEvent()
	{
		var shoppingList = CreateValidShoppingList();

		shoppingList.CheckAllItems();

		shoppingList.UncommittedEvents.OfType<AllItemsChecked>().Should().ContainSingle();
	}

	[Fact]
	public void UncheckAllItems_UnchecksAllItems()
	{
		List<ShoppingListItem> items =
		[
			ShoppingListItem.Create(ItemName.From("Flour"), Quantity.Of(Amount.From(500), Unit.Gram)),
			ShoppingListItem.Create(ItemName.From("Sugar"), Quantity.Of(Amount.From(200), Unit.Gram))
		];
		var shoppingList = CreateValidShoppingList(items: items);
		shoppingList.CheckAllItems();

		shoppingList.UncheckAllItems();

		shoppingList.Items.Should().OnlyContain(i => !i.IsChecked);
	}

	[Fact]
	public void UncheckAllItems_RaisesAllItemsUncheckedEvent()
	{
		var shoppingList = CreateValidShoppingList();

		shoppingList.UncheckAllItems();

		shoppingList.UncommittedEvents.OfType<AllItemsUnchecked>().Should().ContainSingle();
	}

	[Fact]
	public void ClearRecipeReference_ClearsTheReference()
	{
		var shoppingList = CreateValidShoppingList(recipeReference: RecipeReference.New());

		shoppingList.ClearRecipeReference();

		shoppingList.RecipeReference.Should().BeNull();
	}

	[Fact]
	public void ClearRecipeReference_RaisesRecipeReferenceClearedEvent()
	{
		var shoppingList = CreateValidShoppingList(recipeReference: RecipeReference.New());

		shoppingList.ClearRecipeReference();

		shoppingList.UncommittedEvents.OfType<RecipeReferenceCleared>().Should().ContainSingle();
	}

	[Fact]
	public void CheckOffItem_InvalidItemId_Throws()
	{
		var shoppingList = CreateValidShoppingList();

		var act = () => shoppingList.CheckOffItem(ShoppingListItemIdentifier.New());

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void Version_IncrementsOnEachEvent()
	{
		List<ShoppingListItem> items =
		[
			ShoppingListItem.Create(ItemName.From("Flour"), Quantity.Of(Amount.From(500), Unit.Gram))
		];
		var shoppingList = CreateValidShoppingList(items: items);
		var versionAfterCreate = shoppingList.Version;

		shoppingList.CheckOffItem(items[0].Id);

		shoppingList.Version.Should().Be(versionAfterCreate.Increment());
	}

	[Fact]
	public void FromEvents_ReplaysToSameState()
	{
		List<ShoppingListItem> items =
		[
			ShoppingListItem.Create(ItemName.From("Flour"), Quantity.Of(Amount.From(500), Unit.Gram)),
			ShoppingListItem.Create(ItemName.From("Sugar"), Quantity.Of(Amount.From(200), Unit.Gram))
		];
		var original = CreateValidShoppingList(items: items, recipeReference: RecipeReference.New());
		original.CheckOffItem(items[0].Id);
		original.ClearRecipeReference();
		var capturedEvents = original.UncommittedEvents.ToList();

		var replayed = Domain.ShoppingList.ShoppingList.FromEvents(original.Identifier, capturedEvents);

		replayed.Identifier.Should().Be(original.Identifier);
		replayed.Title.Should().Be(original.Title);
		replayed.Owner.Should().Be(original.Owner);
		replayed.CreatedAt.Should().Be(original.CreatedAt);
		replayed.RecipeReference.Should().BeNull();
		replayed.Items.Should().HaveCount(2);
		replayed.Items.Single(i => i.Id == items[0].Id).IsChecked.Should().BeTrue();
		replayed.Items.Single(i => i.Id == items[1].Id).IsChecked.Should().BeFalse();
		replayed.Version.Should().Be(original.Version);
	}

	[Fact]
	public void MarkCommitted_ClearsUncommittedEvents()
	{
		var shoppingList = CreateValidShoppingList();

		shoppingList.MarkCommitted();

		shoppingList.UncommittedEvents.Should().BeEmpty();
	}

	private static Domain.ShoppingList.ShoppingList CreateValidShoppingList(
		ShoppingListTitle? title = null,
		OwnerIdentifier? owner = null,
		IReadOnlyList<ShoppingListItem>? items = null,
		RecipeReference? recipeReference = null)
	{
		return Domain.ShoppingList.ShoppingList.Create(
			title ?? ShoppingListTitle.From("Test Shopping List"),
			owner ?? OwnerIdentifier.From("user-123"),
			items ?? [ShoppingListItem.Create(ItemName.From("Flour"), Quantity.Of(Amount.From(500), Unit.Gram))],
			recipeReference);
	}
}
