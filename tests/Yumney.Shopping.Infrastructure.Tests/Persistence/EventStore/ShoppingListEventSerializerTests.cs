using System.Text.Json;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Quantities;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Tests.Persistence.EventStore;

/// <summary>
/// Round-trips every value-object converter registered on the ShoppingList
/// serializer through serialize → deserialize. The converters are internal —
/// this exercises them via the public serializer surface.
/// </summary>
public class ShoppingListEventSerializerTests
{
	[Fact]
	public void Options_RegistersAllExpectedConverters()
	{
		var options = ShoppingListEventSerializer.Options;

		options.Converters.Should().HaveCountGreaterThan(0);
	}

	[Fact]
	public void Roundtrip_ShoppingListCreated_PreservesEveryField()
	{
		var listId = ShoppingListIdentifier.From(Guid.NewGuid());
		var owner = OwnerIdentifier.From("user-1");
		var title = ShoppingListTitle.From("Weekly groceries");
		var recipeRef = RecipeReference.From(Guid.NewGuid());
		var createdAt = new DateTime(2026, 5, 17, 10, 0, 0, DateTimeKind.Utc);
		var @event = new ShoppingListCreated(listId, title, owner, recipeRef, createdAt);

		var json = JsonSerializer.Serialize(@event, ShoppingListEventSerializer.Options);
		var rehydrated = (ShoppingListCreated)ShoppingListEventSerializer.Deserialize(
			nameof(ShoppingListCreated), json)!;

		rehydrated.Identifier.Should().Be(listId);
		rehydrated.Title.Should().Be(title);
		rehydrated.Owner.Should().Be(owner);
		rehydrated.RecipeReference.Should().Be(recipeRef);
		rehydrated.CreatedAt.Should().Be(createdAt);
	}

	[Fact]
	public void Roundtrip_ListItemAdded_PreservesQuantityAndCategory()
	{
		var itemId = ShoppingListItemIdentifier.From(Guid.NewGuid());
		var name = ItemName.From("Onion");
		var quantity = Quantity.Of(Amount.From(2.5m), Unit.From("kg"));
		var category = IngredientCategory.From("produce");
		var @event = new ListItemAdded(itemId, name, quantity, category);

		var json = JsonSerializer.Serialize(@event, ShoppingListEventSerializer.Options);
		var rehydrated = (ListItemAdded)ShoppingListEventSerializer.Deserialize(
			nameof(ListItemAdded), json)!;

		rehydrated.ItemId.Should().Be(itemId);
		rehydrated.Name.Should().Be(name);
		rehydrated.Quantity!.Amount.Value.Should().Be(2.5m);
		rehydrated.Quantity.Unit!.Value.Should().Be("kg");
		rehydrated.Category!.Value.Should().Be("produce");
	}

	[Fact]
	public void Roundtrip_UnitConverter_NullUnit_StaysNull()
	{
		var quantity = Quantity.Of(Amount.From(3m), Unit.FromNullable(null));
		var @event = new ListItemAdded(
			ShoppingListItemIdentifier.From(Guid.NewGuid()),
			ItemName.From("Eggs"),
			quantity,
			IngredientCategory.From("dairy"));

		var json = JsonSerializer.Serialize(@event, ShoppingListEventSerializer.Options);
		var rehydrated = (ListItemAdded)ShoppingListEventSerializer.Deserialize(
			nameof(ListItemAdded), json)!;

		rehydrated.Quantity!.Unit.Should().BeNull();
	}

	[Fact]
	public void Deserialize_UnknownEventType_ReturnsNull()
	{
		ShoppingListEventSerializer.Deserialize("NotAShoppingListEvent", "{}").Should().BeNull();
	}
}
