using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger.Events;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.Tests.ShoppingLedger;

#pragma warning disable SA1601
public partial class ShoppingLedgerTests
#pragma warning restore SA1601
{
	[Fact]
	public void UndoBought_ReversesBoughtQuantity()
	{
		var milk = N("Milk");
		var quantity = Q(2, "L");
		var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(Owner("user-123"));
		ledger.AddItem(milk, quantity, ItemSource.Manual);
		ledger.MarkBought(milk, quantity);

		ledger.UndoBought(milk, quantity);

		var item = ledger.Items.Values.First();
		item.Bought.Should().Be(Amount.From(0));
		item.IsBought.Should().BeFalse();
	}

	[Fact]
	public void UndoBought_PartialUndo()
	{
		var milk = N("Milk");
		var quantity = Q(3, "L");
		var undoQuantity = Q(1, "L");
		var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(Owner("user-123"));
		ledger.AddItem(milk, quantity, ItemSource.Manual);
		ledger.MarkBought(milk, quantity);

		ledger.UndoBought(milk, undoQuantity);

		ledger.Items.Values.First().Bought.Should().Be(Amount.From(2));
	}

	[Fact]
	public void UndoBought_ExceedsBought_ThrowsBusinessRuleValidation()
	{
		var milk = N("Milk");
		var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(Owner("user-123"));
		ledger.AddItem(milk, Q(1, "L"), ItemSource.Manual);

		var act = () => ledger.UndoBought(milk, Q(5, "L"));

		act.Should().Throw<BusinessRuleValidationException>()
			.WithMessage("Cannot undo more than the recorded bought quantity.");
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	public void UndoBought_EmptyItemName_ThrowsGuardException(string? itemName)
	{
		var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(Owner("user-123"));

		var act = () => ledger.UndoBought(N(itemName!), Q(1, "L"));

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void AddAsAtHome_AddsDirectlyToBought()
	{
		var butter = N("Butter");
		var quantity = Q(250, "g");
		var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(Owner("user-123"));

		ledger.AddAsAtHome(butter, quantity);

		var item = ledger.Items.Values.First();
		item.Bought.Should().Be(quantity.Amount);
		item.OnList.Should().Be(Amount.From(0));
		item.AtHome.Should().Be(quantity.Amount);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	public void AddAsAtHome_EmptyItemName_ThrowsGuardException(string? itemName)
	{
		var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(Owner("user-123"));

		var act = () => ledger.AddAsAtHome(N(itemName!), Q(1, "pc"));

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void FromEvents_RebuildsUndoBoughtState()
	{
		var owner = Owner("user-123");
		var milk = N("Milk");
		var quantity = Q(2, "L");
		var original = Domain.ShoppingLedger.ShoppingLedger.Create(owner);
		original.AddItem(milk, quantity, ItemSource.Manual);
		original.MarkBought(milk, quantity);
		original.UndoBought(milk, quantity);

		var events = original.UncommittedEvents.ToList();
		var rebuilt = Domain.ShoppingLedger.ShoppingLedger.FromEvents(original.Identifier, owner, events);

		rebuilt.Items.Values.First().Bought.Should().Be(Amount.From(0));
	}

	[Fact]
	public void FromEvents_RebuildsAddAsAtHomeState()
	{
		var owner = Owner("user-123");
		var butter = N("Butter");
		var quantity = Q(250, "g");
		var original = Domain.ShoppingLedger.ShoppingLedger.Create(owner);
		original.AddAsAtHome(butter, quantity);

		var events = original.UncommittedEvents.ToList();
		var rebuilt = Domain.ShoppingLedger.ShoppingLedger.FromEvents(original.Identifier, owner, events);

		rebuilt.Items.Values.First().AtHome.Should().Be(quantity.Amount);
	}

	[Fact]
	public void MarkAsFrozen_RaisesEventWithoutTouchingState()
	{
		var milk = N("Milk");
		var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(Owner("user-123"));
		ledger.AddAsAtHome(milk, Q(2, "L"));
		var atHomeBefore = ledger.Items.Values.Single().AtHome;

		ledger.MarkAsFrozen(milk, Unit.From("L"));

		ledger.UncommittedEvents.OfType<ShoppingItemMarkedAsFrozen>().Should().ContainSingle();
		ledger.Items.Values.Single().AtHome.Should().Be(atHomeBefore);
	}

	[Fact]
	public void MarkAsFrozen_NullUnit_RaisesEventWithNullUnit()
	{
		var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(Owner("user-123"));

		ledger.MarkAsFrozen(N("Eggs"), null);

		var raised = ledger.UncommittedEvents.OfType<ShoppingItemMarkedAsFrozen>().Single();
		raised.Unit.Should().BeNull();
	}

	[Fact]
	public void FromEvents_RebuildsMarkAsFrozenWithoutCorruptingState()
	{
		var owner = Owner("user-123");
		var butter = N("Butter");
		var quantity = Q(250, "g");
		var original = Domain.ShoppingLedger.ShoppingLedger.Create(owner);
		original.AddAsAtHome(butter, quantity);
		original.MarkAsFrozen(butter, Unit.From("g"));

		var events = original.UncommittedEvents.ToList();
		var rebuilt = Domain.ShoppingLedger.ShoppingLedger.FromEvents(original.Identifier, owner, events);

		rebuilt.Items.Values.Single().AtHome.Should().Be(quantity.Amount);
	}
}
