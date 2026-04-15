using FluentAssertions;
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
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");

        ledger.OwnerId.Should().Be("user-123");
        ledger.Items.Should().BeEmpty();
        ledger.Version.Should().Be(0);
        ledger.UncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public void AddItem_RaisesEventAndUpdatesState()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");

        ledger.AddItem(N("Milk"), A(1), U("L"), "manual");

        ledger.UncommittedEvents.Should().HaveCount(1);
        ledger.UncommittedEvents.First().Should().BeOfType<ShoppingItemAdded>();
        ledger.Version.Should().Be(1);
        ledger.Items.Should().HaveCount(1);
    }

    [Fact]
    public void AddItem_UpdatesItemState()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");

        ledger.AddItem(N("Milk"), A(2), U("L"), "manual");

        var item = ledger.Items.Values.First();
        item.ItemName.Value.Should().Be("Milk");
        item.OnList.Should().Be(2);
        item.Unit!.Value.Should().Be("L");
    }

    [Fact]
    public void MarkBought_UpdatesBoughtQuantity()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");
        ledger.AddItem(N("Milk"), A(2), U("L"), "manual");

        ledger.MarkBought(N("Milk"), A(2), U("L"));

        var item = ledger.Items.Values.First();
        item.Bought.Should().Be(2);
        item.IsBought.Should().BeTrue();
        item.AtHome.Should().Be(2);
    }

    [Fact]
    public void MarkConsumed_ReducesAtHome()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");
        ledger.AddItem(N("Milk"), A(2), U("L"), "manual");
        ledger.MarkBought(N("Milk"), A(2), U("L"));

        ledger.MarkConsumed(N("Milk"), A(1), U("L"), "recipe:abc");

        var item = ledger.Items.Values.First();
        item.Consumed.Should().Be(1);
        item.AtHome.Should().Be(1);
    }

    [Fact]
    public void RemoveItem_ReducesAtHome()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");
        ledger.AddItem(N("Milk"), A(2), U("L"), "manual");
        ledger.MarkBought(N("Milk"), A(2), U("L"));

        ledger.RemoveItem(N("Milk"), A(1), U("L"), "spoiled");

        var item = ledger.Items.Values.First();
        item.Removed.Should().Be(1);
        item.AtHome.Should().Be(1);
    }

    [Fact]
    public void AdjustQuantity_OverridesOnList()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");
        ledger.AddItem(N("Milk"), A(2), U("L"), "manual");

        ledger.AdjustQuantity(N("Milk"), A(5), U("L"));

        var item = ledger.Items.Values.First();
        item.OnList.Should().Be(5);
    }

    [Fact]
    public void MultipleAdds_SameItem_SumsQuantity()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");

        ledger.AddItem(N("Milk"), A(1), U("L"), "manual");
        ledger.AddItem(N("Milk"), A(2), U("L"), "recipe:abc");

        ledger.Items.Should().HaveCount(1);
        ledger.Items.Values.First().OnList.Should().Be(3);
    }

    [Fact]
    public void DifferentUnits_SeparateItems()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");

        ledger.AddItem(N("Milk"), A(2), U("cups"), "manual");
        ledger.AddItem(N("Milk"), A(500), U("ml"), "manual");

        ledger.Items.Should().HaveCount(2);
    }

    [Fact]
    public void CaseInsensitiveMerge()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");

        ledger.AddItem(N("milk"), A(1), U("L"), "manual");
        ledger.AddItem(N("MILK"), A(1), U("L"), "manual");

        ledger.Items.Should().HaveCount(1);
        ledger.Items.Values.First().OnList.Should().Be(2);
    }

    [Fact]
    public void AtHome_NeverNegative()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");
        ledger.AddItem(N("Milk"), A(1), U("L"), "manual");

        ledger.MarkConsumed(N("Milk"), A(5), U("L"), "manual");

        ledger.Items.Values.First().AtHome.Should().Be(0);
    }

    [Fact]
    public void MarkCommitted_ClearsUncommittedEvents()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");
        ledger.AddItem(N("Milk"), A(1), U("L"), "manual");

        ledger.MarkCommitted();

        ledger.UncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public void FromEvents_RebuildsSameState()
    {
        var original = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");
        original.AddItem(N("Milk"), A(2), U("L"), "manual");
        original.MarkBought(N("Milk"), A(2), U("L"));
        original.MarkConsumed(N("Milk"), A(1), U("L"), "recipe:abc");

        var events = original.UncommittedEvents.ToList();
        var rebuilt = Domain.ShoppingLedger.ShoppingLedger.FromEvents(original.Id, "user-123", events);

        rebuilt.Items.Should().HaveCount(1);
        var item = rebuilt.Items.Values.First();
        item.OnList.Should().Be(2);
        item.Bought.Should().Be(2);
        item.Consumed.Should().Be(1);
        item.AtHome.Should().Be(1);
        rebuilt.Version.Should().Be(3);
    }

    [Fact]
    public void FromSnapshot_ContinuesFromSnapshotState()
    {
        var original = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");
        original.AddItem(N("Milk"), A(2), U("L"), "manual");
        original.MarkBought(N("Milk"), A(2), U("L"));

        var snapshotItems = original.Items.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        var newEvents = new[] { new ShoppingItemConsumed(N("Milk"), A(1), U("L"), "recipe:abc") };

        var rebuilt = Domain.ShoppingLedger.ShoppingLedger.FromSnapshot(
            original.Id, "user-123", snapshotItems, 2, newEvents);

        rebuilt.Version.Should().Be(3);
        var item = rebuilt.Items.Values.First();
        item.AtHome.Should().Be(1);
    }

    [Fact]
    public void Version_IncrementsPerEvent()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");

        ledger.AddItem(N("A"), A(1), null, "manual");
        ledger.AddItem(N("B"), A(1), null, "manual");
        ledger.AddItem(N("C"), A(1), null, "manual");

        ledger.Version.Should().Be(3);
        ledger.UncommittedEvents.Should().HaveCount(3);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddItem_EmptyItemName_ThrowsGuardException(string? itemName)
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");

        var act = () => ledger.AddItem(N(itemName!), A(1), U("L"), "manual");

        act.Should().Throw<GuardException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddItem_EmptySource_ThrowsGuardException(string? source)
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");

        var act = () => ledger.AddItem(N("Milk"), A(1), U("L"), source!);

        act.Should().Throw<GuardException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void MarkBought_EmptyItemName_ThrowsGuardException(string? itemName)
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");

        var act = () => ledger.MarkBought(N(itemName!), A(1), U("L"));

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void StartShoppingMode_SetsIsInShoppingMode()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");

        ledger.StartShoppingMode();

        ledger.IsInShoppingMode.Should().BeTrue();
        ledger.ShoppingModeStartedAt.Should().NotBeNull();
        ledger.PendingChangesCount.Should().Be(0);
    }

    [Fact]
    public void StartShoppingMode_AlreadyInMode_NoOp()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");
        ledger.StartShoppingMode();
        var version = ledger.Version;

        ledger.StartShoppingMode();

        ledger.Version.Should().Be(version);
    }

    [Fact]
    public void EndShoppingMode_ClearsShoppingState()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");
        ledger.StartShoppingMode();

        ledger.EndShoppingMode(acceptPendingChanges: true);

        ledger.IsInShoppingMode.Should().BeFalse();
        ledger.ShoppingModeStartedAt.Should().BeNull();
    }

    [Fact]
    public void EndShoppingMode_NotInMode_NoOp()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");
        var version = ledger.Version;

        ledger.EndShoppingMode(acceptPendingChanges: false);

        ledger.Version.Should().Be(version);
    }

    [Fact]
    public void AddItem_WhileInShoppingMode_IncrementsPendingChanges()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");
        ledger.AddItem(N("Milk"), A(1), U("L"), "manual");
        ledger.StartShoppingMode();

        ledger.AddItem(N("Eggs"), A(6), U("pc"), "manual");
        ledger.AddItem(N("Bread"), A(1), U("pc"), "manual");

        ledger.PendingChangesCount.Should().Be(2);
    }

    [Fact]
    public void EndShoppingMode_ResetsPendingChanges()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");
        ledger.StartShoppingMode();
        ledger.AddItem(N("Eggs"), A(6), U("pc"), "manual");

        ledger.EndShoppingMode(acceptPendingChanges: false);

        ledger.PendingChangesCount.Should().Be(0);
    }

    [Fact]
    public void RemoveItem_WhileInShoppingMode_IncrementsPendingChanges()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");
        ledger.AddItem(N("Milk"), A(2), U("L"), "manual");
        ledger.StartShoppingMode();

        ledger.RemoveItem(N("Milk"), A(1), U("L"));

        ledger.PendingChangesCount.Should().Be(1);
    }

    [Fact]
    public void AdjustQuantity_WhileInShoppingMode_IncrementsPendingChanges()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");
        ledger.AddItem(N("Milk"), A(2), U("L"), "manual");
        ledger.StartShoppingMode();

        ledger.AdjustQuantity(N("Milk"), A(5), U("L"));

        ledger.PendingChangesCount.Should().Be(1);
    }

    [Fact]
    public void MarkBought_WhileInShoppingMode_DoesNotIncrementPendingChanges()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");
        ledger.AddItem(N("Milk"), A(2), U("L"), "manual");
        ledger.StartShoppingMode();

        ledger.MarkBought(N("Milk"), A(2), U("L"));

        ledger.PendingChangesCount.Should().Be(0);
    }

    [Fact]
    public void AddItem_NotInShoppingMode_DoesNotTrackPending()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");

        ledger.AddItem(N("Milk"), A(1), U("L"), "manual");

        ledger.PendingChangesCount.Should().Be(0);
    }

    [Fact]
    public void FromEvents_RebuildShoppingModeState()
    {
        var original = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");
        original.AddItem(N("Milk"), A(1), U("L"), "manual");
        original.StartShoppingMode();
        original.AddItem(N("Eggs"), A(6), U("pc"), "manual");

        var events = original.UncommittedEvents.ToList();
        var rebuilt = Domain.ShoppingLedger.ShoppingLedger.FromEvents(original.Id, "user-123", events);

        rebuilt.IsInShoppingMode.Should().BeTrue();
        rebuilt.ShoppingModeStartedAt.Should().NotBeNull();
        rebuilt.PendingChangesCount.Should().Be(1);
    }

    [Fact]
    public void FromEvents_RebuildEndedShoppingMode()
    {
        var original = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");
        original.StartShoppingMode();
        original.AddItem(N("Eggs"), A(6), U("pc"), "manual");
        original.EndShoppingMode(acceptPendingChanges: true);

        var events = original.UncommittedEvents.ToList();
        var rebuilt = Domain.ShoppingLedger.ShoppingLedger.FromEvents(original.Id, "user-123", events);

        rebuilt.IsInShoppingMode.Should().BeFalse();
        rebuilt.PendingChangesCount.Should().Be(0);
    }

    [Fact]
    public void StartShoppingMode_RaisesCorrectEvent()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");

        ledger.StartShoppingMode();

        ledger.UncommittedEvents.Should().HaveCount(1);
        ledger.UncommittedEvents.First().Should().BeOfType<ShoppingModeStarted>();
    }

    [Fact]
    public void EndShoppingMode_RaisesCorrectEvent()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");
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
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");
        ledger.AddItem(N("Milk"), A(2), U("L"), "manual");
        ledger.MarkBought(N("Milk"), A(2), U("L"));

        ledger.UndoBought(N("Milk"), A(2), U("L"));

        var item = ledger.Items.Values.First();
        item.Bought.Should().Be(0);
        item.IsBought.Should().BeFalse();
    }

    [Fact]
    public void UndoBought_PartialUndo()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");
        ledger.AddItem(N("Milk"), A(3), U("L"), "manual");
        ledger.MarkBought(N("Milk"), A(3), U("L"));

        ledger.UndoBought(N("Milk"), A(1), U("L"));

        ledger.Items.Values.First().Bought.Should().Be(2);
    }

    [Fact]
    public void UndoBought_NeverNegative()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");
        ledger.AddItem(N("Milk"), A(1), U("L"), "manual");

        ledger.UndoBought(N("Milk"), A(5), U("L"));

        ledger.Items.Values.First().Bought.Should().Be(0);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void UndoBought_EmptyItemName_ThrowsGuardException(string? itemName)
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");

        var act = () => ledger.UndoBought(N(itemName!), A(1), U("L"));

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void AddAsAtHome_AddsDirectlyToBought()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");

        ledger.AddAsAtHome(N("Butter"), A(250), U("g"));

        var item = ledger.Items.Values.First();
        item.Bought.Should().Be(250);
        item.OnList.Should().Be(0);
        item.AtHome.Should().Be(250);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void AddAsAtHome_EmptyItemName_ThrowsGuardException(string? itemName)
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");

        var act = () => ledger.AddAsAtHome(N(itemName!), A(1), U("pc"));

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void FromEvents_RebuildsUndoBoughtState()
    {
        var original = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");
        original.AddItem(N("Milk"), A(2), U("L"), "manual");
        original.MarkBought(N("Milk"), A(2), U("L"));
        original.UndoBought(N("Milk"), A(2), U("L"));

        var events = original.UncommittedEvents.ToList();
        var rebuilt = Domain.ShoppingLedger.ShoppingLedger.FromEvents(original.Id, "user-123", events);

        rebuilt.Items.Values.First().Bought.Should().Be(0);
    }

    [Fact]
    public void FromEvents_RebuildsAddAsAtHomeState()
    {
        var original = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");
        original.AddAsAtHome(N("Butter"), A(250), U("g"));

        var events = original.UncommittedEvents.ToList();
        var rebuilt = Domain.ShoppingLedger.ShoppingLedger.FromEvents(original.Id, "user-123", events);

        rebuilt.Items.Values.First().AtHome.Should().Be(250);
    }

    private static ItemName N(string name) => ItemName.From(name);

    private static Amount A(decimal amount) => Amount.From(amount);

    private static Unit U(string unit) => Unit.From(unit);
}
