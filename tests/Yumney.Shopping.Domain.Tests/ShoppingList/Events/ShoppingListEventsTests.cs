using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Quantities;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.Tests.ShoppingList.Events;

/// <summary>
/// Construction + field-stamping tests for every ShoppingList domain event.
/// Records get free Equals / GetHashCode from the compiler; DomainEvent's
/// OccurredOn timestamp makes deep equality non-deterministic, so tests
/// assert per-property values instead.
/// </summary>
public class ShoppingListEventsTests
{
	private static readonly ShoppingListItemIdentifier ItemId = ShoppingListItemIdentifier.From(Guid.NewGuid());

	[Fact]
	public void ShoppingListCreated_PositionalCtor_StampsAllFields()
	{
		var listId = ShoppingListIdentifier.From(Guid.NewGuid());
		var title = ShoppingListTitle.From("Weekly groceries");
		var owner = OwnerIdentifier.From("kc-user-1");
		var recipeRef = RecipeReference.From(Guid.NewGuid());
		var createdAt = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc);

		var @event = new ShoppingListCreated(listId, title, owner, recipeRef, createdAt);

		@event.Identifier.Should().Be(listId);
		@event.Title.Should().Be(title);
		@event.Owner.Should().Be(owner);
		@event.RecipeReference.Should().Be(recipeRef);
		@event.CreatedAt.Should().Be(createdAt);
	}

	[Fact]
	public void ShoppingListCreated_NullRecipeReference_IsAllowed()
	{
		var @event = new ShoppingListCreated(
			ShoppingListIdentifier.From(Guid.NewGuid()),
			ShoppingListTitle.From("Manual list"),
			OwnerIdentifier.From("kc-user-1"),
			RecipeReference: null,
			DateTime.UtcNow);

		@event.RecipeReference.Should().BeNull();
	}

	[Fact]
	public void ListItemAdded_AllFields_StampsThrough()
	{
		var name = ItemName.From("Milk");
		var quantity = Quantity.Of(Amount.From(1m), Unit.From("l"));
		var category = IngredientCategory.Dairy;

		var @event = new ListItemAdded(ItemId, name, quantity, category);

		@event.ItemId.Should().Be(ItemId);
		@event.Name.Should().Be(name);
		@event.Quantity.Should().Be(quantity);
		@event.Category.Should().Be(category);
	}

	[Fact]
	public void ListItemAdded_NullQuantity_IsAllowed()
	{
		var @event = new ListItemAdded(ItemId, ItemName.From("Bread"), Quantity: null);

		@event.Quantity.Should().BeNull();
		@event.Category.Should().BeNull();
	}

	[Fact]
	public void ListItemChecked_StampsItemId()
	{
		var @event = new ListItemChecked(ItemId);

		@event.ItemId.Should().Be(ItemId);
	}

	[Fact]
	public void ListItemUnchecked_StampsItemId()
	{
		var @event = new ListItemUnchecked(ItemId);

		@event.ItemId.Should().Be(ItemId);
	}

	[Fact]
	public void ListItemCategoryChanged_StampsItemAndCategory()
	{
		var @event = new ListItemCategoryChanged(ItemId, IngredientCategory.Produce);

		@event.ItemId.Should().Be(ItemId);
		@event.Category.Should().Be(IngredientCategory.Produce);
	}

	[Fact]
	public void AllItemsChecked_ParameterlessCtor_Works()
	{
		var @event = new AllItemsChecked();

		@event.Should().NotBeNull();
	}

	[Fact]
	public void AllItemsUnchecked_ParameterlessCtor_Works()
	{
		var @event = new AllItemsUnchecked();

		@event.Should().NotBeNull();
	}

	[Fact]
	public void RecipeReferenceCleared_ParameterlessCtor_Works()
	{
		var @event = new RecipeReferenceCleared();

		@event.Should().NotBeNull();
	}
}
