using FluentAssertions;
using FluentAssertions.Execution;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger.Events;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.Tests.ShoppingLedger;

public class ShoppingLedgerTests
{
	[Fact]
	public void Create_ValidOwner_ReturnsEmptyLedger()
	{
		var owner = Owner("user-123");

		var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(owner);

		using var scope = new AssertionScope();
		ledger.OwnerId.Should().Be(owner);
		ledger.Items.Should().BeEmpty();
		ledger.Version.Should().Be(AggregateVersion.Zero());
		ledger.UncommittedEvents.Should().BeEmpty();
	}

	[Fact]
	public void AddItem_RaisesEventAndUpdatesState()
	{
		var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(Owner("user-123"));

		ledger.AddItem(N("Milk"), Q(1, "L"), ItemSource.Manual);

		ledger.UncommittedEvents.Should().HaveCount(1);
		ledger.UncommittedEvents.First().Should().BeOfType<ShoppingItemAdded>();
		ledger.Version.Should().Be(AggregateVersion.From(1));
		ledger.Items.Should().HaveCount(1);
	}

	[Fact]
	public void AddItem_UpdatesItemState()
	{
		var milk = N("Milk");
		var quantity = Q(2, "L");
		var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(Owner("user-123"));

		ledger.AddItem(milk, quantity, ItemSource.Manual);

		var item = ledger.Items.Values.First();
		item.ItemName.Should().Be(milk);
		item.OnList.Should().Be(quantity.Amount);
		item.Unit.Should().Be(quantity.Unit);
	}

	[Fact]
	public void MarkBought_UpdatesBoughtQuantity()
	{
		var milk = N("Milk");
		var quantity = Q(2, "L");
		var expectedAmount = Amount.From(2);
		var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(Owner("user-123"));
		ledger.AddItem(milk, quantity, ItemSource.Manual);

		ledger.MarkBought(milk, quantity);

		var item = ledger.Items.Values.First();
		item.Bought.Should().Be(expectedAmount);
		item.IsBought.Should().BeTrue();
		item.AtHome.Should().Be(expectedAmount);
	}

	[Fact]
	public void MarkConsumed_ReducesAtHome()
	{
		var milk = N("Milk");
		var quantity = Q(2, "L");
		var consumedQuantity = Q(1, "L");
		var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(Owner("user-123"));
		ledger.AddItem(milk, quantity, ItemSource.Manual);
		ledger.MarkBought(milk, quantity);

		ledger.MarkConsumed(milk, consumedQuantity, ItemSource.From("recipe:abc"));

		var item = ledger.Items.Values.First();
		item.Consumed.Should().Be(consumedQuantity.Amount);
		item.AtHome.Should().Be(Amount.From(1));
	}

	[Fact]
	public void RemoveItem_ReducesAtHome()
	{
		var milk = N("Milk");
		var quantity = Q(2, "L");
		var removeQuantity = Q(1, "L");
		var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(Owner("user-123"));
		ledger.AddItem(milk, quantity, ItemSource.Manual);
		ledger.MarkBought(milk, quantity);

		ledger.RemoveItem(milk, removeQuantity, RemovalReason.From("spoiled"));

		var item = ledger.Items.Values.First();
		item.Removed.Should().Be(removeQuantity.Amount);
		item.AtHome.Should().Be(Amount.From(1));
	}

	[Fact]
	public void AdjustQuantity_OverridesOnList()
	{
		var milk = N("Milk");
		var adjustedQuantity = Q(5, "L");
		var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(Owner("user-123"));
		ledger.AddItem(milk, Q(2, "L"), ItemSource.Manual);

		ledger.AdjustQuantity(milk, adjustedQuantity);

		var item = ledger.Items.Values.First();
		item.OnList.Should().Be(adjustedQuantity.Amount);
	}

	[Fact]
	public void MultipleAdds_SameItem_SumsQuantity()
	{
		var milk = N("Milk");
		var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(Owner("user-123"));

		ledger.AddItem(milk, Q(1, "L"), ItemSource.Manual);
		ledger.AddItem(milk, Q(2, "L"), ItemSource.From("recipe:abc"));

		ledger.Items.Should().HaveCount(1);
		ledger.Items.Values.First().OnList.Should().Be(Amount.From(3));
	}

	[Fact]
	public void DifferentUnits_SeparateItems()
	{
		var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(Owner("user-123"));

		ledger.AddItem(N("Milk"), Q(2, "cups"), ItemSource.Manual);
		ledger.AddItem(N("Milk"), Q(500, "ml"), ItemSource.Manual);

		ledger.Items.Should().HaveCount(2);
	}

	[Fact]
	public void CaseInsensitiveMerge()
	{
		var quantity = Q(1, "L");
		var expectedSum = Amount.From(2);
		var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(Owner("user-123"));

		ledger.AddItem(N("milk"), quantity, ItemSource.Manual);
		ledger.AddItem(N("MILK"), quantity, ItemSource.Manual);

		ledger.Items.Should().HaveCount(1);
		ledger.Items.Values.First().OnList.Should().Be(expectedSum);
	}

	[Fact]
	public void AtHome_NeverNegative()
	{
		var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(Owner("user-123"));
		ledger.AddItem(N("Milk"), Q(1, "L"), ItemSource.Manual);

		ledger.MarkConsumed(N("Milk"), Q(5, "L"), ItemSource.Manual);

		ledger.Items.Values.First().AtHome.Should().Be(Amount.From(0));
	}

	[Fact]
	public void MarkCommitted_ClearsUncommittedEvents()
	{
		var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(Owner("user-123"));
		ledger.AddItem(N("Milk"), Q(1, "L"), ItemSource.Manual);

		ledger.MarkCommitted();

		ledger.UncommittedEvents.Should().BeEmpty();
	}

	[Fact]
	public void FromEvents_RebuildsSameState()
	{
		var owner = Owner("user-123");
		var milk = N("Milk");
		var quantity = Q(2, "L");
		var consumedQuantity = Q(1, "L");
		var original = Domain.ShoppingLedger.ShoppingLedger.Create(owner);
		original.AddItem(milk, quantity, ItemSource.Manual);
		original.MarkBought(milk, quantity);
		original.MarkConsumed(milk, consumedQuantity, ItemSource.From("recipe:abc"));

		var events = original.UncommittedEvents.ToList();
		var rebuilt = Domain.ShoppingLedger.ShoppingLedger.FromEvents(original.Identifier, owner, events);

		rebuilt.Items.Should().HaveCount(1);
		var item = rebuilt.Items.Values.First();
		item.OnList.Should().Be(quantity.Amount);
		item.Bought.Should().Be(quantity.Amount);
		item.Consumed.Should().Be(consumedQuantity.Amount);
		item.AtHome.Should().Be(consumedQuantity.Amount);
		rebuilt.Version.Should().Be(AggregateVersion.From(3));
	}

	[Fact]
	public void Version_IncrementsPerEvent()
	{
		var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(Owner("user-123"));

		ledger.AddItem(N("A"), Q(1), ItemSource.Manual);
		ledger.AddItem(N("B"), Q(1), ItemSource.Manual);
		ledger.AddItem(N("C"), Q(1), ItemSource.Manual);

		ledger.Version.Should().Be(AggregateVersion.From(3));
		ledger.UncommittedEvents.Should().HaveCount(3);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void AddItem_EmptyItemName_ThrowsGuardException(string? itemName)
	{
		var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(Owner("user-123"));

		var act = () => ledger.AddItem(N(itemName!), Q(1, "L"), ItemSource.Manual);

		act.Should().Throw<GuardException>();
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void AddItem_EmptySource_ThrowsGuardException(string? source)
	{
		var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(Owner("user-123"));

		var act = () => ledger.AddItem(N("Milk"), Q(1, "L"), ItemSource.From(source!));

		act.Should().Throw<GuardException>();
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	public void MarkBought_EmptyItemName_ThrowsGuardException(string? itemName)
	{
		var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(Owner("user-123"));

		var act = () => ledger.MarkBought(N(itemName!), Q(1, "L"));

		act.Should().Throw<GuardException>();
	}

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

	private static ItemName N(string name) => ItemName.From(name);

	private static Quantity Q(decimal amount, string? unit = null) =>
		Quantity.Of(Amount.From(amount), Unit.FromNullable(unit));

	private static OwnerIdentifier Owner(string id) => OwnerIdentifier.From(id);
}
