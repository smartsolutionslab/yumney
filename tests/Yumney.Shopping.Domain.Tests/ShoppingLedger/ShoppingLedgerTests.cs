using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger.Events;
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

        ledger.AddItem("Milk", 1, "L", "manual");

        ledger.UncommittedEvents.Should().HaveCount(1);
        ledger.UncommittedEvents.First().Should().BeOfType<ShoppingItemAdded>();
        ledger.Version.Should().Be(1);
        ledger.Items.Should().HaveCount(1);
    }

    [Fact]
    public void AddItem_UpdatesItemState()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");

        ledger.AddItem("Milk", 2, "L", "manual");

        var item = ledger.Items.Values.First();
        item.ItemName.Should().Be("Milk");
        item.OnList.Should().Be(2);
        item.Unit.Should().Be("L");
    }

    [Fact]
    public void MarkBought_UpdatesBoughtQuantity()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");
        ledger.AddItem("Milk", 2, "L", "manual");

        ledger.MarkBought("Milk", 2, "L");

        var item = ledger.Items.Values.First();
        item.Bought.Should().Be(2);
        item.IsBought.Should().BeTrue();
        item.AtHome.Should().Be(2);
    }

    [Fact]
    public void MarkConsumed_ReducesAtHome()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");
        ledger.AddItem("Milk", 2, "L", "manual");
        ledger.MarkBought("Milk", 2, "L");

        ledger.MarkConsumed("Milk", 1, "L", "recipe:abc");

        var item = ledger.Items.Values.First();
        item.Consumed.Should().Be(1);
        item.AtHome.Should().Be(1);
    }

    [Fact]
    public void RemoveItem_ReducesAtHome()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");
        ledger.AddItem("Milk", 2, "L", "manual");
        ledger.MarkBought("Milk", 2, "L");

        ledger.RemoveItem("Milk", 1, "L", "spoiled");

        var item = ledger.Items.Values.First();
        item.Removed.Should().Be(1);
        item.AtHome.Should().Be(1);
    }

    [Fact]
    public void AdjustQuantity_OverridesOnList()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");
        ledger.AddItem("Milk", 2, "L", "manual");

        ledger.AdjustQuantity("Milk", 5, "L");

        var item = ledger.Items.Values.First();
        item.OnList.Should().Be(5);
    }

    [Fact]
    public void MultipleAdds_SameItem_SumsQuantity()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");

        ledger.AddItem("Milk", 1, "L", "manual");
        ledger.AddItem("Milk", 2, "L", "recipe:abc");

        ledger.Items.Should().HaveCount(1);
        ledger.Items.Values.First().OnList.Should().Be(3);
    }

    [Fact]
    public void DifferentUnits_SeparateItems()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");

        ledger.AddItem("Milk", 2, "cups", "manual");
        ledger.AddItem("Milk", 500, "ml", "manual");

        ledger.Items.Should().HaveCount(2);
    }

    [Fact]
    public void CaseInsensitiveMerge()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");

        ledger.AddItem("milk", 1, "L", "manual");
        ledger.AddItem("MILK", 1, "L", "manual");

        ledger.Items.Should().HaveCount(1);
        ledger.Items.Values.First().OnList.Should().Be(2);
    }

    [Fact]
    public void AtHome_NeverNegative()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");
        ledger.AddItem("Milk", 1, "L", "manual");

        ledger.MarkConsumed("Milk", 5, "L", "manual");

        ledger.Items.Values.First().AtHome.Should().Be(0);
    }

    [Fact]
    public void MarkCommitted_ClearsUncommittedEvents()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");
        ledger.AddItem("Milk", 1, "L", "manual");

        ledger.MarkCommitted();

        ledger.UncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public void FromEvents_RebuildsSameState()
    {
        var original = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");
        original.AddItem("Milk", 2, "L", "manual");
        original.MarkBought("Milk", 2, "L");
        original.MarkConsumed("Milk", 1, "L", "recipe:abc");

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
        original.AddItem("Milk", 2, "L", "manual");
        original.MarkBought("Milk", 2, "L");

        var snapshotItems = original.Items.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        var newEvents = new[] { new ShoppingItemConsumed("Milk", 1, "L", "recipe:abc") };

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

        ledger.AddItem("A", 1, null, "manual");
        ledger.AddItem("B", 1, null, "manual");
        ledger.AddItem("C", 1, null, "manual");

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

        var act = () => ledger.AddItem(itemName!, 1, "L", "manual");

        act.Should().Throw<GuardException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddItem_EmptySource_ThrowsGuardException(string? source)
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");

        var act = () => ledger.AddItem("Milk", 1, "L", source!);

        act.Should().Throw<GuardException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void MarkBought_EmptyItemName_ThrowsGuardException(string? itemName)
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create("user-123");

        var act = () => ledger.MarkBought(itemName!, 1, "L");

        act.Should().Throw<GuardException>();
    }
}
