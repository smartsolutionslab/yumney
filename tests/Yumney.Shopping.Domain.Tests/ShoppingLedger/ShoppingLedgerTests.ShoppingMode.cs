using FluentAssertions;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger.Events;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.Tests.ShoppingLedger;

#pragma warning disable SA1601
public partial class ShoppingLedgerTests
#pragma warning restore SA1601
{
	[Fact]
	public void StartShoppingMode_SetsIsInShoppingMode()
	{
		var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(Owner("user-123"));

		ledger.StartShoppingMode();

		ledger.IsInShoppingMode.Should().BeTrue();
		ledger.ShoppingModeStartedAt.Should().NotBeNull();
		ledger.PendingChangesCount.Should().Be(0);
	}

	[Fact]
	public void StartShoppingMode_AlreadyInMode_NoOp()
	{
		var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(Owner("user-123"));
		ledger.StartShoppingMode();
		var version = ledger.Version;

		ledger.StartShoppingMode();

		ledger.Version.Should().Be(version);
	}

	[Fact]
	public void EndShoppingMode_ClearsShoppingState()
	{
		var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(Owner("user-123"));
		ledger.StartShoppingMode();

		ledger.EndShoppingMode(acceptPendingChanges: true);

		ledger.IsInShoppingMode.Should().BeFalse();
		ledger.ShoppingModeStartedAt.Should().BeNull();
	}

	[Fact]
	public void EndShoppingMode_NotInMode_NoOp()
	{
		var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(Owner("user-123"));
		var version = ledger.Version;

		ledger.EndShoppingMode(acceptPendingChanges: false);

		ledger.Version.Should().Be(version);
	}

	[Fact]
	public void AddItem_WhileInShoppingMode_IncrementsPendingChanges()
	{
		var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(Owner("user-123"));
		ledger.AddItem(N("Milk"), Q(1, "L"), ItemSource.Manual);
		ledger.StartShoppingMode();

		ledger.AddItem(N("Eggs"), Q(6, "pc"), ItemSource.Manual);
		ledger.AddItem(N("Bread"), Q(1, "pc"), ItemSource.Manual);

		ledger.PendingChangesCount.Should().Be(2);
	}

	[Fact]
	public void EndShoppingMode_ResetsPendingChanges()
	{
		var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(Owner("user-123"));
		ledger.StartShoppingMode();
		ledger.AddItem(N("Eggs"), Q(6, "pc"), ItemSource.Manual);

		ledger.EndShoppingMode(acceptPendingChanges: false);

		ledger.PendingChangesCount.Should().Be(0);
	}

	[Fact]
	public void RemoveItem_WhileInShoppingMode_IncrementsPendingChanges()
	{
		var milk = N("Milk");
		var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(Owner("user-123"));
		ledger.AddItem(milk, Q(2, "L"), ItemSource.Manual);
		ledger.StartShoppingMode();

		ledger.RemoveItem(milk, Q(1, "L"));

		ledger.PendingChangesCount.Should().Be(1);
	}

	[Fact]
	public void AdjustQuantity_WhileInShoppingMode_IncrementsPendingChanges()
	{
		var milk = N("Milk");
		var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(Owner("user-123"));
		ledger.AddItem(milk, Q(2, "L"), ItemSource.Manual);
		ledger.StartShoppingMode();

		ledger.AdjustQuantity(milk, Q(5, "L"));

		ledger.PendingChangesCount.Should().Be(1);
	}

	[Fact]
	public void MarkBought_WhileInShoppingMode_DoesNotIncrementPendingChanges()
	{
		var milk = N("Milk");
		var quantity = Q(2, "L");
		var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(Owner("user-123"));
		ledger.AddItem(milk, quantity, ItemSource.Manual);
		ledger.StartShoppingMode();

		ledger.MarkBought(milk, quantity);

		ledger.PendingChangesCount.Should().Be(0);
	}

	[Fact]
	public void AddItem_NotInShoppingMode_DoesNotTrackPending()
	{
		var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(Owner("user-123"));

		ledger.AddItem(N("Milk"), Q(1, "L"), ItemSource.Manual);

		ledger.PendingChangesCount.Should().Be(0);
	}

	[Fact]
	public void FromEvents_RebuildShoppingModeState()
	{
		var owner = Owner("user-123");
		var original = Domain.ShoppingLedger.ShoppingLedger.Create(owner);
		original.AddItem(N("Milk"), Q(1, "L"), ItemSource.Manual);
		original.StartShoppingMode();
		original.AddItem(N("Eggs"), Q(6, "pc"), ItemSource.Manual);

		var events = original.UncommittedEvents.ToList();
		var rebuilt = Domain.ShoppingLedger.ShoppingLedger.FromEvents(original.Identifier, owner, events);

		rebuilt.IsInShoppingMode.Should().BeTrue();
		rebuilt.ShoppingModeStartedAt.Should().NotBeNull();
		rebuilt.PendingChangesCount.Should().Be(1);
	}

	[Fact]
	public void FromEvents_RebuildEndedShoppingMode()
	{
		var owner = Owner("user-123");
		var original = Domain.ShoppingLedger.ShoppingLedger.Create(owner);
		original.StartShoppingMode();
		original.AddItem(N("Eggs"), Q(6, "pc"), ItemSource.Manual);
		original.EndShoppingMode(acceptPendingChanges: true);

		var events = original.UncommittedEvents.ToList();
		var rebuilt = Domain.ShoppingLedger.ShoppingLedger.FromEvents(original.Identifier, owner, events);

		rebuilt.IsInShoppingMode.Should().BeFalse();
		rebuilt.PendingChangesCount.Should().Be(0);
	}

	[Fact]
	public void StartShoppingMode_RaisesCorrectEvent()
	{
		var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(Owner("user-123"));

		ledger.StartShoppingMode();

		ledger.UncommittedEvents.Should().HaveCount(1);
		ledger.UncommittedEvents.First().Should().BeOfType<ShoppingModeStarted>();
	}

	[Fact]
	public void EndShoppingMode_RaisesCorrectEvent()
	{
		var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(Owner("user-123"));
		ledger.StartShoppingMode();

		ledger.EndShoppingMode(acceptPendingChanges: false);

		ledger.UncommittedEvents.Should().HaveCount(2);
		var endEvent = ledger.UncommittedEvents.Last() as ShoppingModeEnded;
		endEvent.Should().NotBeNull();
		endEvent!.AcceptedPendingChanges.Should().BeFalse();
	}
}
