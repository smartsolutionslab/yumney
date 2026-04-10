using FluentAssertions;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.Tests.ShoppingLedger;

public class MergedShoppingItemTests
{
    private static readonly OwnerIdentifier TestOwner = OwnerIdentifier.From("user-123");

    [Fact]
    public void GetMergedItems_SameItemSameUnit_SumsQuantity()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(TestOwner);
        ledger.AddItem(ItemName.From("Milk"), 2, "L", TransactionSource.Manual);
        ledger.AddItem(ItemName.From("Milk"), 1, "L", TransactionSource.FromRecipe(Guid.NewGuid()));

        var merged = ledger.GetMergedItems();

        merged.Should().HaveCount(1);
        merged[0].ItemName.Should().Be("Milk");
        merged[0].TotalQuantity.Should().Be(3);
        merged[0].Unit.Should().Be("L");
    }

    [Fact]
    public void GetMergedItems_SameItemDifferentUnit_SeparateLines()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(TestOwner);
        ledger.AddItem(ItemName.From("Milk"), 2, "cups", TransactionSource.Manual);
        ledger.AddItem(ItemName.From("Milk"), 500, "ml", TransactionSource.Manual);

        var merged = ledger.GetMergedItems();

        merged.Should().HaveCount(2);
    }

    [Fact]
    public void GetMergedItems_DifferentItems_SeparateLines()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(TestOwner);
        ledger.AddItem(ItemName.From("Flour"), 200, "g", TransactionSource.Manual);
        ledger.AddItem(ItemName.From("Sugar"), 300, "g", TransactionSource.Manual);

        var merged = ledger.GetMergedItems();

        merged.Should().HaveCount(2);
    }

    [Fact]
    public void GetMergedItems_SourcesTracked()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(TestOwner);
        var recipeSource = TransactionSource.FromRecipe(Guid.NewGuid());
        ledger.AddItem(ItemName.From("Flour"), 200, "g", recipeSource);
        ledger.AddItem(ItemName.From("Flour"), 300, "g", TransactionSource.Manual);

        var merged = ledger.GetMergedItems();

        merged[0].Sources.Should().HaveCount(2);
        merged[0].Sources[0].Quantity.Should().Be(200);
        merged[0].Sources[1].Quantity.Should().Be(300);
    }

    [Fact]
    public void GetMergedItems_CaseInsensitiveMerge()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(TestOwner);
        ledger.AddItem(ItemName.From("milk"), 1, "L", TransactionSource.Manual);
        ledger.AddItem(ItemName.From("MILK"), 1, "L", TransactionSource.Manual);

        var merged = ledger.GetMergedItems();

        merged.Should().HaveCount(1);
        merged[0].TotalQuantity.Should().Be(2);
    }

    [Fact]
    public void GetMergedItems_BoughtItemMarkedAsBought()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(TestOwner);
        ledger.AddItem(ItemName.From("Eggs"), 6, "pc", TransactionSource.Manual);
        ledger.MarkBought(ItemName.From("Eggs"), 6, "pc");

        var merged = ledger.GetMergedItems();

        merged[0].IsBought.Should().BeTrue();
    }

    [Fact]
    public void GetMergedItems_UnboughtItemNotMarked()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(TestOwner);
        ledger.AddItem(ItemName.From("Eggs"), 6, "pc", TransactionSource.Manual);

        var merged = ledger.GetMergedItems();

        merged[0].IsBought.Should().BeFalse();
    }

    [Fact]
    public void GetMergedItems_EmptyLedger_ReturnsEmpty()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(TestOwner);

        var merged = ledger.GetMergedItems();

        merged.Should().BeEmpty();
    }

    [Fact]
    public void GetMergedItems_OnlyAddsProjected_NotOtherActions()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(TestOwner);
        ledger.AddItem(ItemName.From("Milk"), 2, "L", TransactionSource.Manual);
        ledger.MarkBought(ItemName.From("Milk"), 2, "L");
        ledger.MarkConsumed(ItemName.From("Milk"), 1, "L", TransactionSource.Manual);

        var merged = ledger.GetMergedItems();

        merged.Should().HaveCount(1);
        merged[0].TotalQuantity.Should().Be(2);
    }
}
