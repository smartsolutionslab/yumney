using FluentAssertions;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger.Events;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.Tests.ShoppingLedger.Events;

/// <summary>
/// Construction + field-stamping tests for every ShoppingLedger domain event.
/// EventSerializationTests covers JSON round-trips; this file pins the
/// positional constructors so a field rename or reorder shows up here.
/// </summary>
public class ShoppingLedgerEventsTests
{
	private static readonly ItemName Milk = ItemName.From("Milk");
	private static readonly Quantity Litre = Quantity.Of(Amount.From(1m), Unit.From("l"));

	[Fact]
	public void ShoppingItemAdded_StampsAllFields()
	{
		var @event = new ShoppingItemAdded(Milk, Litre, ItemSource.Manual);

		@event.ItemName.Should().Be(Milk);
		@event.Quantity.Should().Be(Litre);
		@event.Source.Should().Be(ItemSource.Manual);
	}

	[Fact]
	public void ShoppingItemAddedAsAtHome_StampsItemAndQuantity()
	{
		var @event = new ShoppingItemAddedAsAtHome(Milk, Litre);

		@event.ItemName.Should().Be(Milk);
		@event.Quantity.Should().Be(Litre);
	}

	[Fact]
	public void ShoppingItemBought_StampsItemAndQuantity()
	{
		var @event = new ShoppingItemBought(Milk, Litre);

		@event.ItemName.Should().Be(Milk);
		@event.Quantity.Should().Be(Litre);
	}

	[Fact]
	public void ShoppingItemConsumed_StampsAllFields()
	{
		var @event = new ShoppingItemConsumed(Milk, Litre, ItemSource.MealPlan);

		@event.ItemName.Should().Be(Milk);
		@event.Quantity.Should().Be(Litre);
		@event.Source.Should().Be(ItemSource.MealPlan);
	}

	[Fact]
	public void ShoppingItemMarkedAsFrozen_StampsItemAndUnit()
	{
		var unit = Unit.From("kg");

		var @event = new ShoppingItemMarkedAsFrozen(Milk, unit);

		@event.ItemName.Should().Be(Milk);
		@event.Unit.Should().Be(unit);
	}

	[Fact]
	public void ShoppingItemMarkedAsFrozen_NullUnit_IsAllowed()
	{
		var @event = new ShoppingItemMarkedAsFrozen(Milk, Unit: null);

		@event.Unit.Should().BeNull();
	}

	[Fact]
	public void ShoppingItemQuantityAdjusted_StampsItemAndNewQuantity()
	{
		var newQty = Quantity.Of(Amount.From(2m), Unit.From("l"));

		var @event = new ShoppingItemQuantityAdjusted(Milk, newQty);

		@event.ItemName.Should().Be(Milk);
		@event.NewQuantity.Should().Be(newQty);
	}

	[Fact]
	public void ShoppingItemRemoved_WithReason_StampsAllFields()
	{
		var reason = RemovalReason.From("out of stock");

		var @event = new ShoppingItemRemoved(Milk, Litre, reason);

		@event.ItemName.Should().Be(Milk);
		@event.Quantity.Should().Be(Litre);
		@event.Reason.Should().Be(reason);
	}

	[Fact]
	public void ShoppingItemRemoved_NullReason_IsAllowed()
	{
		var @event = new ShoppingItemRemoved(Milk, Litre, Reason: null);

		@event.Reason.Should().BeNull();
	}

	[Fact]
	public void ShoppingItemUndoBought_StampsItemAndQuantity()
	{
		var @event = new ShoppingItemUndoBought(Milk, Litre);

		@event.ItemName.Should().Be(Milk);
		@event.Quantity.Should().Be(Litre);
	}

	[Fact]
	public void ShoppingModeStarted_StampsSnapshotTimestamp()
	{
		var snapshotAt = new DateTime(2026, 5, 15, 9, 30, 0, DateTimeKind.Utc);

		var @event = new ShoppingModeStarted(snapshotAt);

		@event.SnapshotTakenAt.Should().Be(snapshotAt);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void ShoppingModeEnded_StampsAcceptedPendingChanges(bool accepted)
	{
		var @event = new ShoppingModeEnded(accepted);

		@event.AcceptedPendingChanges.Should().Be(accepted);
	}
}
