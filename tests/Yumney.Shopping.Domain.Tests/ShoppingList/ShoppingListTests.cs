using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;
using SmartSolutionsLab.Yumney.Shopping.Domain.Tests.Builders;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.Tests.ShoppingList;

public class ShoppingListTests
{
	[Fact]
	public void Create_ValidInput_CreatesShoppingListWithIdentifier()
	{
		var shoppingList = ShoppingListBuilder.A().Build();

		shoppingList.Identifier.Should().NotBeNull();
	}

	[Fact]
	public void Create_ValidInput_SetsTitle()
	{
		var shoppingList = ShoppingListBuilder.A().WithTitle("Weekly Groceries").Build();

		shoppingList.Title.Should().Be(ShoppingListTitle.From("Weekly Groceries"));
	}

	[Fact]
	public void Create_ValidInput_SetsOwner()
	{
		var shoppingList = ShoppingListBuilder.A().OwnedBy("user-123").Build();

		shoppingList.Owner.Should().Be(OwnerIdentifier.From("user-123"));
	}

	[Fact]
	public void Create_ValidInput_SetsCreatedAtCloseToNow()
	{
		var before = DateTime.UtcNow;

		var shoppingList = ShoppingListBuilder.A().Build();

		shoppingList.CreatedAt.Should().BeCloseTo(before, TimeSpan.FromSeconds(5));
	}

	[Fact]
	public void Create_ValidInput_SetsItems()
	{
		var shoppingList = ShoppingListBuilder.A()
			.WithItems([
				ShoppingListItemBuilder.A().Named("Flour").WithQuantity(500, Unit.Gram),
				ShoppingListItemBuilder.A().Named("Sugar").WithQuantity(200, Unit.Gram),
			])
			.Build();

		shoppingList.Items.Should().HaveCount(2);
	}

	[Fact]
	public void Create_WithRecipeReference_SetsRecipeReference()
	{
		var recipeReference = RecipeReference.New();
		var shoppingList = ShoppingListBuilder.A().FromRecipe(recipeReference.Value).Build();

		shoppingList.RecipeReference.Should().Be(recipeReference);
	}

	[Fact]
	public void Create_WithoutRecipeReference_RecipeReferenceIsNull()
	{
		var shoppingList = ShoppingListBuilder.A().Build();

		shoppingList.RecipeReference.Should().BeNull();
	}

	[Fact]
	public void Create_EmptyItems_ThrowsGuardException()
	{
		// Cannot use the builder — Build() seeds a default item to keep happy
		// paths concise. Construct directly to test the guard.
		var act = () => Domain.ShoppingList.ShoppingList.Create(
			ShoppingListTitle.From("Test Shopping List"),
			OwnerIdentifier.From("user-123"),
			items: []);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void Create_RaisesShoppingListCreatedEvent()
	{
		var shoppingList = ShoppingListBuilder.A().WithTitle("Groceries").Build();

		shoppingList.UncommittedEvents.OfType<ShoppingListCreated>().Should().ContainSingle()
			.Which.Title.Should().Be(ShoppingListTitle.From("Groceries"));
	}

	[Fact]
	public void Create_ShoppingListCreatedEvent_ContainsShoppingListIdentifier()
	{
		var shoppingList = ShoppingListBuilder.A().Build();

		var domainEvent = shoppingList.UncommittedEvents.OfType<ShoppingListCreated>().Should().ContainSingle().Subject;

		domainEvent.Identifier.Should().Be(shoppingList.Identifier);
	}

	[Fact]
	public void Create_RaisesListItemAddedEventPerItem()
	{
		var shoppingList = ShoppingListBuilder.A()
			.WithItems([
				ShoppingListItemBuilder.A().Named("Flour").WithQuantity(500, Unit.Gram),
				ShoppingListItemBuilder.A().Named("Sugar").WithQuantity(200, Unit.Gram),
			])
			.Build();

		shoppingList.UncommittedEvents.OfType<ListItemAdded>().Should().HaveCount(2);
	}

	[Fact]
	public void Create_GeneratesUniqueIdentifiers()
	{
		var list1 = ShoppingListBuilder.A().Build();
		var list2 = ShoppingListBuilder.A().Build();

		list1.Identifier.Should().NotBe(list2.Identifier);
	}

	[Fact]
	public void CheckOffItem_ChecksTheItem()
	{
		var item = ShoppingListItemBuilder.A().Named("Flour").WithQuantity(500, Unit.Gram).Build();
		var shoppingList = ShoppingListBuilder.A().WithItems([item]).Build();

		shoppingList.CheckOffItem(item.Id);

		shoppingList.Items[0].IsChecked.Should().BeTrue();
	}

	[Fact]
	public void CheckOffItem_RaisesListItemCheckedEvent()
	{
		var item = ShoppingListItemBuilder.A().Named("Flour").WithQuantity(500, Unit.Gram).Build();
		var shoppingList = ShoppingListBuilder.A().WithItems([item]).Build();

		shoppingList.CheckOffItem(item.Id);

		shoppingList.UncommittedEvents.OfType<ListItemChecked>().Should().ContainSingle()
			.Which.ItemId.Should().Be(item.Id);
	}

	[Fact]
	public void UncheckItem_UnchecksTheItem()
	{
		var item = ShoppingListItemBuilder.A().Named("Flour").WithQuantity(500, Unit.Gram).Build();
		var shoppingList = ShoppingListBuilder.A().WithItems([item]).Build();
		shoppingList.CheckOffItem(item.Id);

		shoppingList.UncheckItem(item.Id);

		shoppingList.Items[0].IsChecked.Should().BeFalse();
	}

	[Fact]
	public void UncheckItem_RaisesListItemUncheckedEvent()
	{
		var item = ShoppingListItemBuilder.A().Named("Flour").WithQuantity(500, Unit.Gram).Build();
		var shoppingList = ShoppingListBuilder.A().WithItems([item]).Build();

		shoppingList.UncheckItem(item.Id);

		shoppingList.UncommittedEvents.OfType<ListItemUnchecked>().Should().ContainSingle()
			.Which.ItemId.Should().Be(item.Id);
	}

	[Fact]
	public void CheckAllItems_ChecksAllItems()
	{
		var shoppingList = ShoppingListBuilder.A()
			.WithItems([
				ShoppingListItemBuilder.A().Named("Flour").WithQuantity(500, Unit.Gram),
				ShoppingListItemBuilder.A().Named("Sugar").WithQuantity(200, Unit.Gram),
			])
			.Build();

		shoppingList.CheckAllItems();

		shoppingList.Items.Should().OnlyContain(i => i.IsChecked);
	}

	[Fact]
	public void CheckAllItems_RaisesAllItemsCheckedEvent()
	{
		var shoppingList = ShoppingListBuilder.A().Build();

		shoppingList.CheckAllItems();

		shoppingList.UncommittedEvents.OfType<AllItemsChecked>().Should().ContainSingle();
	}

	[Fact]
	public void UncheckAllItems_UnchecksAllItems()
	{
		var shoppingList = ShoppingListBuilder.A()
			.WithItems([
				ShoppingListItemBuilder.A().Named("Flour").WithQuantity(500, Unit.Gram),
				ShoppingListItemBuilder.A().Named("Sugar").WithQuantity(200, Unit.Gram),
			])
			.Build();
		shoppingList.CheckAllItems();

		shoppingList.UncheckAllItems();

		shoppingList.Items.Should().OnlyContain(i => !i.IsChecked);
	}

	[Fact]
	public void UncheckAllItems_RaisesAllItemsUncheckedEvent()
	{
		var shoppingList = ShoppingListBuilder.A().Build();

		shoppingList.UncheckAllItems();

		shoppingList.UncommittedEvents.OfType<AllItemsUnchecked>().Should().ContainSingle();
	}

	[Fact]
	public void ClearRecipeReference_ClearsTheReference()
	{
		var shoppingList = ShoppingListBuilder.A().FromRecipe(Guid.NewGuid()).Build();

		shoppingList.ClearRecipeReference();

		shoppingList.RecipeReference.Should().BeNull();
	}

	[Fact]
	public void ClearRecipeReference_RaisesRecipeReferenceClearedEvent()
	{
		var shoppingList = ShoppingListBuilder.A().FromRecipe(Guid.NewGuid()).Build();

		shoppingList.ClearRecipeReference();

		shoppingList.UncommittedEvents.OfType<RecipeReferenceCleared>().Should().ContainSingle();
	}

	[Fact]
	public void CheckOffItem_InvalidItemId_Throws()
	{
		var shoppingList = ShoppingListBuilder.A().Build();

		var act = () => shoppingList.CheckOffItem(ShoppingListItemIdentifier.New());

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void Version_IncrementsOnEachEvent()
	{
		var item = ShoppingListItemBuilder.A().Named("Flour").WithQuantity(500, Unit.Gram).Build();
		var shoppingList = ShoppingListBuilder.A().WithItems([item]).Build();
		var versionAfterCreate = shoppingList.Version;

		shoppingList.CheckOffItem(item.Id);

		shoppingList.Version.Should().Be(versionAfterCreate.Increment());
	}

	[Fact]
	public void FromEvents_ReplaysToSameState()
	{
		var flour = ShoppingListItemBuilder.A().Named("Flour").WithQuantity(500, Unit.Gram).Build();
		var sugar = ShoppingListItemBuilder.A().Named("Sugar").WithQuantity(200, Unit.Gram).Build();
		var original = ShoppingListBuilder.A()
			.WithItems([flour, sugar])
			.FromRecipe(Guid.NewGuid())
			.Build();
		original.CheckOffItem(flour.Id);
		original.ClearRecipeReference();
		var capturedEvents = original.UncommittedEvents.ToList();

		var replayed = Domain.ShoppingList.ShoppingList.FromEvents(original.Identifier, capturedEvents);

		replayed.Identifier.Should().Be(original.Identifier);
		replayed.Title.Should().Be(original.Title);
		replayed.Owner.Should().Be(original.Owner);
		replayed.CreatedAt.Should().Be(original.CreatedAt);
		replayed.RecipeReference.Should().BeNull();
		replayed.Items.Should().HaveCount(2);
		replayed.Items.Single(item => item.Id == flour.Id).IsChecked.Should().BeTrue();
		replayed.Items.Single(item => item.Id == sugar.Id).IsChecked.Should().BeFalse();
		replayed.Version.Should().Be(original.Version);
	}

	[Fact]
	public void MarkCommitted_ClearsUncommittedEvents()
	{
		var shoppingList = ShoppingListBuilder.A().Build();

		shoppingList.MarkCommitted();

		shoppingList.UncommittedEvents.Should().BeEmpty();
	}
}
