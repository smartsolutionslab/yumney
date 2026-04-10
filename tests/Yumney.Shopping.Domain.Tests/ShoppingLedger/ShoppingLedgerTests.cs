using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.Tests.ShoppingLedger;

public class ShoppingLedgerTests
{
    private static readonly OwnerIdentifier TestOwner = OwnerIdentifier.From("user-123");

    [Fact]
    public void Create_ValidOwner_ReturnsEmptyLedger()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(TestOwner);

        ledger.Owner.Should().Be(TestOwner);
        ledger.Transactions.Should().BeEmpty();
        ledger.Id.Should().NotBeNull();
    }

    [Fact]
    public void AddItem_RecordsTransaction()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(TestOwner);

        var tx = ledger.AddItem(ItemName.From("Milk"), 1, "L", TransactionSource.Manual);

        ledger.Transactions.Should().HaveCount(1);
        tx.Action.Should().Be(LedgerAction.Added);
        tx.ItemName.Value.Should().Be("Milk");
        tx.Quantity.Should().Be(1);
        tx.Unit.Should().Be("L");
        tx.Source.IsManual.Should().BeTrue();
    }

    [Fact]
    public void AddItem_FromRecipe_TracksSource()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(TestOwner);
        var recipeId = Guid.NewGuid();
        var source = TransactionSource.FromRecipe(recipeId, "monday-dinner");

        var tx = ledger.AddItem(ItemName.From("Chicken"), 500, "g", source);

        tx.Source.Value.Should().Contain(recipeId.ToString());
        tx.Source.Value.Should().Contain("monday-dinner");
        tx.Source.IsManual.Should().BeFalse();
    }

    [Fact]
    public void MarkBought_RecordsTransaction()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(TestOwner);
        ledger.AddItem(ItemName.From("Milk"), 2, "L", TransactionSource.Manual);

        var tx = ledger.MarkBought(ItemName.From("Milk"), 2, "L");

        tx.Action.Should().Be(LedgerAction.Bought);
        ledger.Transactions.Should().HaveCount(2);
    }

    [Fact]
    public void MarkConsumed_RecordsTransaction()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(TestOwner);
        var source = TransactionSource.FromRecipe(Guid.NewGuid());
        ledger.AddItem(ItemName.From("Milk"), 2, "L", source);
        ledger.MarkBought(ItemName.From("Milk"), 2, "L");

        var tx = ledger.MarkConsumed(ItemName.From("Milk"), 1, "L", source);

        tx.Action.Should().Be(LedgerAction.Consumed);
    }

    [Fact]
    public void RemoveItem_RecordsTransactionWithReason()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(TestOwner);
        ledger.AddItem(ItemName.From("Milk"), 1, "L", TransactionSource.Manual);

        var tx = ledger.RemoveItem(ItemName.From("Milk"), 1, "L", "spoiled");

        tx.Action.Should().Be(LedgerAction.Removed);
        tx.Source.Value.Should().Contain("spoiled");
    }

    [Fact]
    public void AdjustQuantity_RecordsTransaction()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(TestOwner);
        ledger.AddItem(ItemName.From("Milk"), 1, "L", TransactionSource.Manual);

        var tx = ledger.AdjustQuantity(ItemName.From("Milk"), 3, "L");

        tx.Action.Should().Be(LedgerAction.Adjusted);
        tx.Quantity.Should().Be(3);
    }

    [Fact]
    public void Rollback_ExistingTransaction_RecordsRollback()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(TestOwner);
        var addTx = ledger.AddItem(ItemName.From("Milk"), 1, "L", TransactionSource.Manual);

        var rollbackTx = ledger.Rollback(addTx.Id);

        rollbackTx.Action.Should().Be(LedgerAction.RolledBack);
        rollbackTx.Source.Value.Should().Contain(addTx.Id.Value.ToString());
        ledger.Transactions.Should().HaveCount(2);
    }

    [Fact]
    public void Rollback_NonExistingTransaction_ThrowsEntityNotFoundException()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(TestOwner);

        var act = () => ledger.Rollback(LedgerTransactionIdentifier.New());

        act.Should().Throw<EntityNotFoundException>();
    }

    [Fact]
    public void CalculateBalance_AddedItem_ShowsOnList()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(TestOwner);
        ledger.AddItem(ItemName.From("Milk"), 2, "L", TransactionSource.Manual);

        var balance = ledger.CalculateBalance();

        balance.Should().HaveCount(1);
        balance[0].ItemName.Should().Be("Milk");
        balance[0].OnList.Should().Be(2);
        balance[0].Bought.Should().Be(0);
        balance[0].AtHome.Should().Be(0);
    }

    [Fact]
    public void CalculateBalance_BoughtItem_ShowsAtHome()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(TestOwner);
        ledger.AddItem(ItemName.From("Milk"), 2, "L", TransactionSource.Manual);
        ledger.MarkBought(ItemName.From("Milk"), 2, "L");

        var balance = ledger.CalculateBalance();

        balance[0].Bought.Should().Be(2);
        balance[0].AtHome.Should().Be(2);
        balance[0].Remaining.Should().Be(0);
    }

    [Fact]
    public void CalculateBalance_ConsumedItem_ReducesAtHome()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(TestOwner);
        var source = TransactionSource.FromRecipe(Guid.NewGuid());
        ledger.AddItem(ItemName.From("Milk"), 2, "L", source);
        ledger.MarkBought(ItemName.From("Milk"), 2, "L");
        ledger.MarkConsumed(ItemName.From("Milk"), 1, "L", source);

        var balance = ledger.CalculateBalance();

        balance[0].AtHome.Should().Be(1);
    }

    [Fact]
    public void CalculateBalance_MultipleAddsOfSameItem_Sums()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(TestOwner);
        ledger.AddItem(ItemName.From("Milk"), 1, "L", TransactionSource.Manual);
        ledger.AddItem(ItemName.From("Milk"), 2, "L", TransactionSource.FromRecipe(Guid.NewGuid()));

        var balance = ledger.CalculateBalance();

        balance.Should().HaveCount(1);
        balance[0].OnList.Should().Be(3);
    }

    [Fact]
    public void CalculateBalance_MultipleItems_TrackedSeparately()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(TestOwner);
        ledger.AddItem(ItemName.From("Milk"), 1, "L", TransactionSource.Manual);
        ledger.AddItem(ItemName.From("Eggs"), 6, "pc", TransactionSource.Manual);

        var balance = ledger.CalculateBalance();

        balance.Should().HaveCount(2);
    }

    [Fact]
    public void CalculateBalance_AtHomeNeverNegative()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(TestOwner);
        ledger.AddItem(ItemName.From("Milk"), 1, "L", TransactionSource.Manual);
        ledger.MarkConsumed(ItemName.From("Milk"), 5, "L", TransactionSource.Manual);

        var balance = ledger.CalculateBalance();

        balance[0].AtHome.Should().Be(0);
    }

    [Fact]
    public void CalculateBalance_RollbackReversesPrevious()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(TestOwner);
        var addTx = ledger.AddItem(ItemName.From("Milk"), 2, "L", TransactionSource.Manual);
        ledger.Rollback(addTx.Id);

        var balance = ledger.CalculateBalance();

        balance[0].OnList.Should().Be(0);
    }

    [Fact]
    public void TransactionSource_Manual_IsManual()
    {
        TransactionSource.Manual.IsManual.Should().BeTrue();
    }

    [Fact]
    public void TransactionSource_FromRecipe_IsNotManual()
    {
        var source = TransactionSource.FromRecipe(Guid.NewGuid());

        source.IsManual.Should().BeFalse();
    }

    [Fact]
    public void AllTransactions_PreservedInOrder()
    {
        var ledger = Domain.ShoppingLedger.ShoppingLedger.Create(TestOwner);
        ledger.AddItem(ItemName.From("Milk"), 1, "L", TransactionSource.Manual);
        ledger.MarkBought(ItemName.From("Milk"), 1, "L");
        ledger.MarkConsumed(ItemName.From("Milk"), 1, "L", TransactionSource.Manual);

        ledger.Transactions.Should().HaveCount(3);
        ledger.Transactions[0].Action.Should().Be(LedgerAction.Added);
        ledger.Transactions[1].Action.Should().Be(LedgerAction.Bought);
        ledger.Transactions[2].Action.Should().Be(LedgerAction.Consumed);
    }
}
