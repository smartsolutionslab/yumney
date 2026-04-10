using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Application.Queries;
using SmartSolutionsLab.Yumney.Shopping.Application.Queries.Handlers;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Tests.Queries;

public class GetMergedShoppingListQueryHandlerTests
{
    private readonly IShoppingLedgerRepository ledgers = Substitute.For<IShoppingLedgerRepository>();
    private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
    private readonly GetMergedShoppingListQueryHandler handler;

    public GetMergedShoppingListQueryHandlerTests()
    {
        currentUser.UserId.Returns("user-123");
        handler = new GetMergedShoppingListQueryHandler(ledgers, currentUser);
    }

    [Fact]
    public async Task HandleAsync_NoLedger_ReturnsEmptyList()
    {
        ledgers.FindByOwnerAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<CancellationToken>())
            .Returns((ShoppingLedger?)null);

        var result = await handler.HandleAsync(new GetMergedShoppingListQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_EmptyLedger_ReturnsEmptyList()
    {
        var ledger = ShoppingLedger.Create(OwnerIdentifier.From("user-123"));
        ledgers.FindByOwnerAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<CancellationToken>())
            .Returns(ledger);

        var result = await handler.HandleAsync(new GetMergedShoppingListQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WithItems_ReturnsCategories()
    {
        var ledger = ShoppingLedger.Create(OwnerIdentifier.From("user-123"));
        ledger.AddItem(ItemName.From("Chicken"), 500, "g", TransactionSource.Manual);
        ledger.AddItem(ItemName.From("Milk"), 1, "L", TransactionSource.Manual);
        ledgers.FindByOwnerAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<CancellationToken>())
            .Returns(ledger);

        var result = await handler.HandleAsync(new GetMergedShoppingListQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);

        var chicken = result.Value.Items.First(i => i.ItemName == "Chicken");
        chicken.Category.Should().Be("meat-fish");

        var milk = result.Value.Items.First(i => i.ItemName == "Milk");
        milk.Category.Should().Be("dairy");
    }

    [Fact]
    public async Task HandleAsync_ItemsOrderedByCategoryDisplayOrder()
    {
        var ledger = ShoppingLedger.Create(OwnerIdentifier.From("user-123"));
        ledger.AddItem(ItemName.From("Toilet Paper"), 1, "pc", TransactionSource.Manual);
        ledger.AddItem(ItemName.From("Onion"), 1, "pc", TransactionSource.Manual);
        ledger.AddItem(ItemName.From("Milk"), 1, "L", TransactionSource.Manual);
        ledgers.FindByOwnerAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<CancellationToken>())
            .Returns(ledger);

        var result = await handler.HandleAsync(new GetMergedShoppingListQuery());

        var categories = result.Value.Items.Select(i => i.Category).ToList();
        categories[0].Should().Be("produce");
        categories[1].Should().Be("dairy");
        categories[2].Should().Be("household");
    }

    [Fact]
    public async Task HandleAsync_UnknownItem_CategoryDefaultsToOther()
    {
        var ledger = ShoppingLedger.Create(OwnerIdentifier.From("user-123"));
        ledger.AddItem(ItemName.From("Tahini"), 1, "pc", TransactionSource.Manual);
        ledgers.FindByOwnerAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<CancellationToken>())
            .Returns(ledger);

        var result = await handler.HandleAsync(new GetMergedShoppingListQuery());

        result.Value.Items[0].Category.Should().Be("other");
    }

    [Fact]
    public async Task HandleAsync_MergedItems_IncludeSources()
    {
        var ledger = ShoppingLedger.Create(OwnerIdentifier.From("user-123"));
        ledger.AddItem(ItemName.From("Flour"), 200, "g", TransactionSource.FromRecipe(Guid.NewGuid()));
        ledger.AddItem(ItemName.From("Flour"), 300, "g", TransactionSource.Manual);
        ledgers.FindByOwnerAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<CancellationToken>())
            .Returns(ledger);

        var result = await handler.HandleAsync(new GetMergedShoppingListQuery());

        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].TotalQuantity.Should().Be(500);
        result.Value.Items[0].Sources.Should().HaveCount(2);
    }
}
